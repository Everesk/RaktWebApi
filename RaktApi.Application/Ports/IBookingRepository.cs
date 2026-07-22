using RaktApi.Domain;

namespace RaktApi.Application.Ports;

/// <summary>
/// Предоставляет доступ к данным бронирований.
/// </summary>
public interface IBookingRepository
{
    /// <summary>Создает бронирование и резервирует место для события.</summary>
    Task<Booking> CreateForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Возвращает бронирование по идентификатору без отслеживания изменений.</summary>
    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>Возвращает бронирования указанного события.</summary>
    Task<IReadOnlyCollection<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Возвращает идентификаторы бронирований, ожидающих обработки.</summary>
    Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Подтверждает бронирование либо отклоняет его при отсутствии события.</summary>
    Task<BookingConfirmationResult> ConfirmAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>Отклоняет бронирование и возвращает место, если это необходимо.</summary>
    Task<bool> TryRejectAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
