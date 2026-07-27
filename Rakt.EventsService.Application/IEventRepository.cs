using Rakt.EventsService.Domain;
namespace Rakt.EventsService.Application;
/// <summary>Порт хранения событий.</summary>
public interface IEventRepository
{
    Task<PaginatedResult<Event>> GetAllAsync(EventQueryDto query, CancellationToken ct = default);

    Task<Event?> GetAsync(Guid id, CancellationToken ct = default);

    Task<Event?> GetForUpdateAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Возвращает запись обработки брони по её идентификатору.
    /// </summary>
    Task<BookingSeatReservation?> GetReservationAsync(Guid bookingId, CancellationToken ct = default);

    Task AddAsync(Event entity, CancellationToken ct = default);

    /// <summary>
    /// Добавляет запись обработки брони.
    /// </summary>
    Task AddReservationAsync(BookingSeatReservation reservation, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    Task DeleteAsync(Event entity, CancellationToken ct = default);
}
