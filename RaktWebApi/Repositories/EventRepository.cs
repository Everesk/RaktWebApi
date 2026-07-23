using Microsoft.EntityFrameworkCore;
using RaktWebApi.Data;
using RaktWebApi.Models;

namespace RaktWebApi.Repositories;

/// <summary>
/// Репозиторий для доступа к событиям через EF Core.
/// </summary>
public sealed class EventRepository(AppDbContext context) : IEventRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Event>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Events.AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Events.AsNoTracking().FirstOrDefaultAsync(eventEntity => eventEntity.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Event?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Events.FirstOrDefaultAsync(eventEntity => eventEntity.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Event eventEntity, CancellationToken cancellationToken = default)
    {
        await context.Events.AddAsync(eventEntity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task DeleteAsync(Event eventEntity, CancellationToken cancellationToken = default)
    {
        context.Events.Remove(eventEntity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
