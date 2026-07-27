using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Application;
using Rakt.EventsService.Domain;
namespace Rakt.EventsService.Infrastructure;
/// <summary>Репозиторий событий EF Core.</summary>
public sealed class EventRepository(EventsDbContext db) : IEventRepository
{
    public async Task<PaginatedResult<Event>> GetAllAsync(EventQueryDto query, CancellationToken ct = default)
    {
        var eventsQuery = db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            eventsQuery = eventsQuery.Where(entity => entity.Title.Contains(query.Title));
        }

        if (query.From.HasValue)
        {
            eventsQuery = eventsQuery.Where(entity => entity.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            eventsQuery = eventsQuery.Where(entity => entity.EndAt <= query.To.Value);
        }

        eventsQuery = eventsQuery.OrderBy(entity => entity.StartAt).ThenBy(entity => entity.Title);
        var totalCount = await eventsQuery.CountAsync(ct);
        var items = query.Page.HasValue && query.PageSize.HasValue
            ? await eventsQuery.Skip((query.Page.Value - 1) * query.PageSize.Value).Take(query.PageSize.Value).ToListAsync(ct)
            : await eventsQuery.ToListAsync(ct);

        return new PaginatedResult<Event> { TotalCount = totalCount, Items = items, Page = query.Page ?? 1, PageSize = query.PageSize ?? items.Count, CurrentCount = items.Count };
    }

    public Task<Event?> GetAsync(Guid id, CancellationToken ct = default) => db.Events.SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<Event?> GetForUpdateAsync(Guid id, CancellationToken ct = default) => db.Events.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<BookingSeatReservation?> GetReservationAsync(Guid bookingId, CancellationToken ct = default) =>
        db.BookingSeatReservations.SingleOrDefaultAsync(reservation => reservation.BookingId == bookingId, ct);
    public Task AddAsync(Event entity, CancellationToken ct = default) => db.Events.AddAsync(entity, ct).AsTask();
    public Task AddReservationAsync(BookingSeatReservation reservation, CancellationToken ct = default) =>
        db.BookingSeatReservations.AddAsync(reservation, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public Task DeleteAsync(Event entity, CancellationToken ct = default)
    {
        db.Events.Remove(entity);
        return Task.CompletedTask;
    }
}
