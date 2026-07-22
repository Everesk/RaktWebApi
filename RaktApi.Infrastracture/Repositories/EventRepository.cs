using Microsoft.EntityFrameworkCore;
using RaktApi.Application.DTO;
using RaktApi.Application.Ports;
using RaktApi.Infrastracture.Data;
using RaktApi.Domain;

namespace RaktApi.Infrastracture.Repositories;

/// <summary>
/// Репозиторий для доступа к событиям через EF Core.
/// </summary>
public sealed class EventRepository(AppDbContext context) : IEventRepository
{
    /// <inheritdoc />
    public async Task<PaginatedResult<Event>> GetAllAsync(EventQueryDto query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var eventsQuery = context.Events.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            var title = query.Title.ToLower();
            eventsQuery = eventsQuery.Where(eventEntity => eventEntity.Title.ToLower().Contains(title));
        }

        if (query.From.HasValue)
        {
            eventsQuery = eventsQuery.Where(eventEntity => eventEntity.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            eventsQuery = eventsQuery.Where(eventEntity => eventEntity.EndAt <= query.To.Value);
        }

        eventsQuery = eventsQuery
            .OrderBy(eventEntity => eventEntity.StartAt)
            .ThenBy(eventEntity => eventEntity.Title)
            .ThenBy(eventEntity => eventEntity.Id);

        var totalCount = await eventsQuery.CountAsync(cancellationToken);
        var items = query.Page.HasValue && query.PageSize.HasValue
            ? await eventsQuery.Skip((query.Page.Value - 1) * query.PageSize.Value).Take(query.PageSize.Value).ToListAsync(cancellationToken)
            : await eventsQuery.ToListAsync(cancellationToken);

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Items = items,
            Page = query.Page ?? 1,
            PageSize = query.PageSize ?? items.Count,
            CurrentCount = items.Count
        };
    }

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
