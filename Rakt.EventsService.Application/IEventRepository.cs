using Rakt.EventsService.Domain;
namespace Rakt.EventsService.Application;
/// <summary>Порт хранения событий.</summary>
public interface IEventRepository { Task<Event?> GetAsync(Guid id, CancellationToken ct = default); Task AddAsync(Event entity, CancellationToken ct = default); Task SaveChangesAsync(CancellationToken ct = default); }
