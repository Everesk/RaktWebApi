using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Интерфейс обработчика бронирований.
/// </summary>
public interface IBookingProcessor
{
    /// <summary>
    /// Обрабатывает бронирование после его обнаружения в статусе Pending.
    /// </summary>
    Task ProcessAsync(Booking booking, CancellationToken cancellationToken = default);
}
