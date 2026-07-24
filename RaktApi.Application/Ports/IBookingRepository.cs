using RaktApi.Domain;

namespace RaktApi.Application.Ports;

/// <summary>
/// Предоставляет доступ к данным бронирований.
/// </summary>
public interface IBookingRepository
{
    /// <summary>Добавляет бронирование и сохраняет изменения.</summary>
    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    /// <summary>Возвращает бронирование по идентификатору без отслеживания изменений.</summary>
    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>Возвращает бронирование для изменения.</summary>
    Task<Booking?> GetForUpdateAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>Возвращает бронирования указанного события.</summary>
    Task<IReadOnlyCollection<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Возвращает количество активных бронирований пользователя.</summary>
    Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Возвращает идентификаторы бронирований, ожидающих обработки.</summary>
    Task<IReadOnlyCollection<Guid>> GetPendingIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Сохраняет изменения бронирования.</summary>
    Task UpdateAsync(CancellationToken cancellationToken = default);
}
