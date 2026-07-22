using RaktApi.Domain;
using RaktApi.Domain.Exceptions;
using RaktApi.Application.Ports;

namespace RaktApi.Application.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public sealed class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;

    /// <summary>
    /// Создает сервис бронирований.
    /// </summary>
    /// <param name="bookingRepository">Репозиторий бронирований.</param>
    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    /// <summary>
    /// Создает бронирование для указанного события.
    /// </summary>
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _bookingRepository.CreateForEventAsync(eventId, cancellationToken);
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

        return await _bookingRepository.GetByEventIdAsync(eventId, cancellationToken);
    }
}
