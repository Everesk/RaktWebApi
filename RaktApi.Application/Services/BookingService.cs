using RaktApi.Domain;
using RaktApi.Domain.Exceptions;
using RaktApi.Application.Ports;

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

    /// <summary>
    /// Создает сервис бронирований.
    /// </summary>
    /// <param name="bookingRepository">Репозиторий бронирований.</param>
    /// <param name="eventRepository">Репозиторий событий.</param>
    public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
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
    /// Создает бронирование без идентификатора пользователя для обратной совместимости.
    /// </summary>
    /// <remarks>Новые вызовы должны использовать перегрузку с идентификатором пользователя.</remarks>
    public Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return CreateBookingAsync(eventId, Guid.Empty, cancellationToken);
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
