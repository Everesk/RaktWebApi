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

    /// <summary>
    /// Отклоняет бронирование и сохраняет его состояние.
    /// </summary>
    Task<bool> TryRejectAsync(Booking booking, CancellationToken cancellationToken = default);
}
