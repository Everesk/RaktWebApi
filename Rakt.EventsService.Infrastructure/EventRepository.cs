using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Application;
using Rakt.EventsService.Domain;
namespace Rakt.EventsService.Infrastructure;
/// <summary>Репозиторий событий EF Core.</summary>
public sealed class EventRepository(EventsDbContext db) : IEventRepository
{
    public Task<Event?> GetAsync(Guid id, CancellationToken ct = default) => db.Events.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddAsync(Event entity, CancellationToken ct = default) => db.Events.AddAsync(entity, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
