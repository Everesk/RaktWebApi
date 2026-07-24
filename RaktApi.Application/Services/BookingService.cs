using RaktApi.Domain;
using RaktApi.Domain.Exceptions;
using RaktApi.Application.Ports;
using RaktApi.Application.Options;
using Microsoft.Extensions.Options;

namespace RaktApi.Application.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public sealed class BookingService : IBookingService
{
    // Синхронизирует создание бронирований внутри экземпляра приложения.
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ICurrentUserContext _currentUserContext;
    // Ограничивает число активных бронирований одного пользователя.
    private readonly int _maxActiveBookings;

    /// <summary>
    /// Создает сервис бронирований.
    /// </summary>
    /// <param name="bookingRepository">Репозиторий бронирований.</param>
    /// <param name="eventRepository">Репозиторий событий.</param>
    public BookingService(
        IBookingRepository bookingRepository,
        IEventRepository eventRepository,
        ICurrentUserContext currentUserContext,
        IOptions<BookingOptions>? bookingOptions = null)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _currentUserContext = currentUserContext;
        _maxActiveBookings = bookingOptions?.Value.MaxActiveBookings ?? new BookingOptions().MaxActiveBookings;
    }

    /// <summary>
    /// Создает бронирование для указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await BookingSemaphore.WaitAsync(cancellationToken);
        try
        {
            var eventEntity = await _eventRepository.GetForUpdateAsync(eventId, cancellationToken)
                ?? throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");

            if (eventEntity.StartAt <= DateTimeOffset.UtcNow)
            {
                throw new PastEventBookingException("Нельзя забронировать уже начавшееся событие.");
            }

            var activeBookingsCount = await _bookingRepository.CountActiveByUserIdAsync(userId, cancellationToken);
            if (activeBookingsCount >= _maxActiveBookings)
            {
                throw new ActiveBookingsLimitExceededException(
                    $"Достигнут лимит активных бронирований: {_maxActiveBookings}.");
            }

            if (!eventEntity.TryReserveSeats())
            {
                throw new NoAvailableSeatsException("Мест нет, уйдите");
            }

            var booking = Booking.Create(eventId, userId);
            await _bookingRepository.AddAsync(booking, cancellationToken);
            return booking;
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    /// <summary>
    /// Отменяет бронирование, если пользователь является его владельцем или администратором.
    /// </summary>
    public async Task CancelBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var booking = await _bookingRepository.GetForUpdateAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException($"Бронь с идентификатором '{bookingId}' не найдена.");

        if (_currentUserContext.Role != UserRole.Admin && booking.UserId != _currentUserContext.UserId)
        {
            throw new OperationForbiddenException("Недостаточно прав для отмены этого бронирования.");
        }

        var shouldReleaseSeat = booking.Status is BookingStatus.Pending or BookingStatus.Confirmed;
        booking.Cancel();

        if (shouldReleaseSeat)
        {
            var eventEntity = await _eventRepository.GetForUpdateAsync(booking.EventId, cancellationToken)
                ?? throw new NotFoundException($"Событие с идентификатором '{booking.EventId}' не найдено.");
            eventEntity.ReleaseSeats();
        }

        await _bookingRepository.UpdateAsync(cancellationToken);
    }

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);

        return booking ?? throw new NotFoundException($"Бронь с идентификатором '{bookingId}' не найдена.");
    }

    /// <summary>
    /// Возвращает все бронирования для указанного события.
    /// </summary>
    public async Task<IReadOnlyCollection<Booking>> GetBookingsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");

        return await _bookingRepository.GetByEventIdAsync(eventEntity.Id, cancellationToken);
    }
}
