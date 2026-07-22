using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Интерфейс сервиса для работы с бронированиями.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создает бронирование для указанного события.
    /// </summary>
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает все бронирования для указанного события.
    /// </summary>
    Task<IReadOnlyCollection<Booking>> GetBookingsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
}
