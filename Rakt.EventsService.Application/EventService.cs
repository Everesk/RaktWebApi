using Rakt.EventsService.Domain;
using Rakt.EventsService.Domain.Exceptions;
using System.Text.Json;
namespace Rakt.EventsService.Application;
/// <summary>Реализует CRUD-сценарии сервиса событий.</summary>
public sealed class EventService(IEventRepository events, ICache cache) : IEventService
{
    private static readonly TimeSpan CacheTimeToLive = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public async Task<PaginatedResult<EventInfoDto>> GetAllAsync(EventQueryDto query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = await events.GetAllAsync(query, ct);
        return new PaginatedResult<EventInfoDto> { TotalCount = result.TotalCount, Items = result.Items.Select(ToDto).ToList(), Page = result.Page, PageSize = result.PageSize, CurrentCount = result.CurrentCount };
    }
    /// <inheritdoc />
    public async Task<EventInfoDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var cacheKey = $"event:{id}";
        var cachedEvent = await GetCachedAsync<EventInfoDto>(cacheKey);
        if (cachedEvent is not null)
        {
            return cachedEvent;
        }

        var entity = await events.GetAsync(id, ct) ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        var result = ToDto(entity);
        await cache.SetAsync(cacheKey, JsonSerializer.Serialize(result), CacheTimeToLive);

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventInfoDto>> GetTopAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        const string cacheKey = "events:top10";
        var cachedEvents = await GetCachedAsync<List<EventInfoDto>>(cacheKey);
        if (cachedEvents is not null)
        {
            return cachedEvents;
        }

        var result = (await events.GetTopAsync(ct)).Select(ToDto).ToList();
        await cache.SetAsync(cacheKey, JsonSerializer.Serialize(result), CacheTimeToLive);

        return result;
    }
    /// <inheritdoc />
    public async Task<EventInfoDto> CreateAsync(CreateEventDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entity = Event.Create(dto.Title, dto.Description, dto.StartAt!.Value, dto.EndAt!.Value, dto.TotalSeats!.Value);
        await events.AddAsync(entity, ct);
        await events.SaveChangesAsync(ct);
        return ToDto(entity);
    }
    /// <inheritdoc />
    public async Task UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await events.GetForUpdateAsync(id, ct) ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        entity.Update(dto.Title, dto.Description, dto.StartAt!.Value, dto.EndAt!.Value);
        await events.SaveChangesAsync(ct);
    }
    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await events.GetForUpdateAsync(id, ct) ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        await events.DeleteAsync(entity, ct);
        await events.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Десериализует значение из кеша или возвращает <see langword="null"/> при его отсутствии либо повреждении.
    /// </summary>
    private async Task<T?> GetCachedAsync<T>(string cacheKey)
    {
        var cachedValue = await cache.GetAsync(cacheKey);
        if (cachedValue is null)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (JsonException)
        {
            await cache.RemoveAsync(cacheKey);
            return default;
        }
    }

    private static EventInfoDto ToDto(Event entity) => new() { Id = entity.Id, Title = entity.Title, Description = entity.Description, StartAt = entity.StartAt, EndAt = entity.EndAt, TotalSeats = entity.TotalSeats, AvailableSeats = entity.AvailableSeats, IsFull = entity.IsFull };
}
