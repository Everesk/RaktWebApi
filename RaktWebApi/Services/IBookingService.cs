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
    Task<Booking> CreateBookingAsync(Guid eventId);

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    Task<Booking> GetBookingByIdAsync(Guid bookingId);
}
