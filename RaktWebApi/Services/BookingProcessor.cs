using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaktWebApi.Data;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Обработчик бронирований.
/// </summary>
public sealed class BookingProcessor : IBookingProcessor
{
    private static readonly TimeSpan ExternalSystemDelay = TimeSpan.FromSeconds(2);
    private static readonly SemaphoreSlim ProcessingSemaphore = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessor> _logger;

    /// <summary>
    /// Создает обработчик бронирований.
    /// </summary>
    /// <param name="scopeFactory">Фабрика scopes.</param>
    /// <param name="logger">Логгер обработчика.</param>
    public BookingProcessor(IServiceScopeFactory scopeFactory, ILogger<BookingProcessor> logger)
    {
        _scopeFactory = scopeFactory;
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

        await ProcessingSemaphore.WaitAsync(cancellationToken);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var trackedBooking = await context.Bookings
                .FirstOrDefaultAsync(item => item.Id == booking.Id, cancellationToken);

            if (trackedBooking is null)
            {
                return;
            }

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
        finally
        {
            ProcessingSemaphore.Release();
        }
    }

    /// <summary>
    /// Отклоняет бронирование и сохраняет его состояние.
    /// </summary>
    public async Task<bool> TryRejectAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            ArgumentNullException.ThrowIfNull(booking);
            _logger.LogWarning("Отклонение брони {BookingId}", booking.Id);

            await ProcessingSemaphore.WaitAsync(cancellationToken);
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var trackedBooking = await context.Bookings
                    .FirstOrDefaultAsync(item => item.Id == booking.Id, cancellationToken);

                if (trackedBooking is null || trackedBooking.Status is BookingStatus.Rejected or BookingStatus.Confirmed)
                {
                    return true;
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
            finally
            {
                ProcessingSemaphore.Release();
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
}
