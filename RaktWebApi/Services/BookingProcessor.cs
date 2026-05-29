using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaktWebApi.Common.Exceptions;
using RaktWebApi.Data;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Обработчик бронирований.
/// </summary>
public sealed class BookingProcessor : IBookingProcessor
{
    private static readonly TimeSpan ExternalSystemDelay = TimeSpan.FromSeconds(2);
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IBookingRepository? _bookingRepository;
    private readonly IEventRepository? _eventRepository;
    private readonly ILogger<BookingProcessor> _logger;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    /// <summary>
    /// Создает обработчик бронирований для работы через EF Core.
    /// </summary>
    /// <param name="scopeFactory">Фабрика scopes.</param>
    /// <param name="logger">Логгер обработчика.</param>
    [ActivatorUtilitiesConstructor]
    public BookingProcessor(IServiceScopeFactory scopeFactory, ILogger<BookingProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Создает обработчик бронирований для работы через in-memory хранилища.
    /// </summary>
    /// <param name="bookingRepository">Хранилище бронирований.</param>
    /// <param name="eventRepository">Хранилище событий.</param>
    /// <param name="logger">Логгер обработчика.</param>
    public BookingProcessor(
        IBookingRepository bookingRepository,
        IEventRepository eventRepository,
        ILogger<BookingProcessor> logger)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    /// <summary>
    /// Обрабатывает бронирование в фоне.
    /// </summary>
    public async Task ProcessAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Начата обработка брони {BookingId}", booking.Id);

        await Task.Delay(ExternalSystemDelay, cancellationToken);

        if (_scopeFactory is not null)
        {
            await ProcessWithContextAsync(booking, cancellationToken);
            return;
        }

        ProcessWithRepositories(booking);
    }

    /// <summary>
    /// Отклоняет бронирование и сохраняет состояние. Безопасен для вызова, может бросить только OperationCanceledException при отмене через CancellationToken.
    /// </summary>
    public async Task<bool> TryRejectAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            ArgumentNullException.ThrowIfNull(booking);
            _logger.LogWarning("Отклонение брони {BookingId}", booking.Id);

            if (_scopeFactory is not null)
            {
                await RejectWithContextAsync(booking, cancellationToken);
            }
            else
            {
                RejectAndReleaseSeats(booking, GetRepositoryEvent(booking.EventId));
            }

            _logger.LogWarning(
                "Бронь {BookingId} переведена в статус {Status}",
                booking.Id,
                booking.Status);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось отклонить бронь {BookingId}", booking.Id);
            return false;
        }
    }

    /// <summary>
    /// Обрабатывает бронирование через DbContext в отдельном scope.
    /// </summary>
    private async Task ProcessWithContextAsync(Booking booking, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var trackedBooking = await context.Bookings
            .FirstOrDefaultAsync(item => item.Id == booking.Id, cancellationToken)
            ?? throw new NotFoundException($"Бронь с идентификатором '{booking.Id}' не найдена.");

        var eventEntity = await context.Events
            .FirstOrDefaultAsync(item => item.Id == trackedBooking.EventId, cancellationToken);

        if (eventEntity is null)
        {
            trackedBooking.Reject(DateTimeOffset.UtcNow);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Бронь {BookingId} отклонена, потому что событие {EventId} удалено",
                trackedBooking.Id,
                trackedBooking.EventId);
            return;
        }

        trackedBooking.Confirm(DateTimeOffset.UtcNow);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Бронь {BookingId} переведена в статус {Status}",
            trackedBooking.Id,
            trackedBooking.Status);
    }

    /// <summary>
    /// Отклоняет бронирование через DbContext в отдельном scope и освобождает места, если событие найдено.
    /// </summary>
    private async Task RejectWithContextAsync(Booking booking, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var trackedBooking = await context.Bookings
            .FirstOrDefaultAsync(item => item.Id == booking.Id, cancellationToken)
            ?? throw new NotFoundException($"Бронь с идентификатором '{booking.Id}' не найдена.");

        if (trackedBooking.Status is BookingStatus.Rejected or BookingStatus.Confirmed)
        {
            return;
        }

        var eventEntity = await context.Events
            .FirstOrDefaultAsync(item => item.Id == trackedBooking.EventId, cancellationToken);

        if (eventEntity is not null)
        {
            eventEntity.ReleaseSeats();
        }

        trackedBooking.Reject(DateTimeOffset.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Обрабатывает бронирование через in-memory хранилища.
    /// </summary>
    private void ProcessWithRepositories(Booking booking)
    {
        Event? eventEntity = _eventRepository!.GetById(booking.EventId);
        if (eventEntity is null)
        {
            booking.Reject(DateTimeOffset.UtcNow);
            _bookingRepository!.Update(booking);

            _logger.LogWarning(
                "Бронь {BookingId} отклонена, потому что событие {EventId} удалено",
                booking.Id,
                booking.EventId);
            return;
        }

        booking.Confirm(DateTimeOffset.UtcNow);
        _bookingRepository!.Update(booking);

        _logger.LogInformation(
            "Бронь {BookingId} переведена в статус {Status}",
            booking.Id,
            booking.Status);
    }

    /// <summary>
    /// Отклоняет бронирование и освобождает места в in-memory хранилищах.
    /// </summary>
    private void RejectAndReleaseSeats(Booking booking, Event? eventEntity)
    {
        if (booking.Status == BookingStatus.Rejected)
        {
            return;
        }

        if (booking.Status == BookingStatus.Confirmed)
        {
            return;
        }

        if (eventEntity is not null)
        {
            eventEntity.ReleaseSeats();
            _eventRepository!.Update(eventEntity);
        }

        booking.Reject(DateTimeOffset.UtcNow);
        _bookingRepository!.Update(booking);
    }

    /// <summary>
    /// Возвращает событие из in-memory хранилища.
    /// </summary>
    private Event? GetRepositoryEvent(Guid eventId)
    {
        return _eventRepository!.GetById(eventId);
    }
}
