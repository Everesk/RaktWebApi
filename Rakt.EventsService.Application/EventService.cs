using Rakt.EventsService.Domain;
using Rakt.EventsService.Domain.Exceptions;
using System.Collections.Concurrent;
using System.Text.Json;
namespace Rakt.EventsService.Application;
/// <summary>Реализует CRUD-сценарии сервиса событий.</summary>
public sealed class EventService(IEventRepository events, ICache cache, CacheOptions cacheOptions) : IEventService
{
    // Синхронизирует прогрев кеша для каждого ключа в пределах экземпляра приложения.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> CacheLocks = new();

    /// <inheritdoc />
    public async Task<PaginatedResult<EventInfoDto>> GetAllAsync(EventQueryDto query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = await events.GetAllAsync(query, ct);
        return new PaginatedResult<EventInfoDto> { TotalCount = result.TotalCount, Items = result.Items.Select(EventInfoDto.FromEntity).ToList(), Page = result.Page, PageSize = result.PageSize, CurrentCount = result.CurrentCount };
    }
    /// <inheritdoc />
    public async Task<EventInfoDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var cacheKey = CacheKeys.SingleEvent(id);
        var cachedEvent = await GetCachedAsync<EventInfoDto>(cacheKey);
        if (cachedEvent is not null)
        {
            return cachedEvent;
        }

        var cacheLock = CacheLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await cacheLock.WaitAsync(ct);
        try
        {
            cachedEvent = await GetCachedAsync<EventInfoDto>(cacheKey);
            if (cachedEvent is not null)
            {
                return cachedEvent;
            }

            var entity = await events.GetAsync(id, ct) ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
            var result = EventInfoDto.FromEntity(entity);
            await UpdateEventCacheAsync(result);

            return result;
        }
        finally
        {
            cacheLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventInfoDto>> GetTopAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var cacheKey = CacheKeys.TopEvents();
        var cachedEvents = await GetCachedAsync<List<EventInfoDto>>(cacheKey);
        if (cachedEvents is not null)
        {
            return cachedEvents;
        }

        var cacheLock = CacheLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await cacheLock.WaitAsync(ct);
        try
        {
            cachedEvents = await GetCachedAsync<List<EventInfoDto>>(cacheKey);
            if (cachedEvents is not null)
            {
                return cachedEvents;
            }

            var result = (await events.GetTopAsync(ct)).Select(EventInfoDto.FromEntity).ToList();
            await cache.SetAsync(
                cacheKey,
                JsonSerializer.Serialize(result),
                GetCacheTimeToLive(cacheOptions.TopEventsTimeToLiveMinutes));

            return result;
        }
        finally
        {
            cacheLock.Release();
        }
    }
    /// <inheritdoc />
    public async Task<EventInfoDto> CreateAsync(CreateEventDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entity = Event.Create(dto.Title, dto.Description, dto.StartAt!.Value, dto.EndAt!.Value, dto.TotalSeats!.Value);
        await events.AddAsync(entity, ct);
        await events.SaveChangesAsync(ct);
        var result = EventInfoDto.FromEntity(entity);
        await UpdateEventCacheAsync(result);

        return result;
    }
    /// <inheritdoc />
    public async Task UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await events.GetForUpdateAsync(id, ct) ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        entity.Update(dto.Title, dto.Description, dto.StartAt!.Value, dto.EndAt!.Value);
        await events.SaveChangesAsync(ct);
        await UpdateEventCacheAsync(EventInfoDto.FromEntity(entity));
    }
    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await events.GetForUpdateAsync(id, ct) ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        await events.DeleteAsync(entity, ct);
        await events.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.SingleEvent(id));
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

    /// <summary>
    /// Возвращает настроенное время жизни записи в кеше.
    /// </summary>
    private static TimeSpan GetCacheTimeToLive(int timeToLiveMinutes) => TimeSpan.FromMinutes(timeToLiveMinutes);

    /// <summary>
    /// Обновляет кеш актуальными данными события после сохранения в базе данных.
    /// </summary>
    private Task UpdateEventCacheAsync(EventInfoDto eventInfo) =>
        cache.SetAsync(
            CacheKeys.SingleEvent(eventInfo.Id),
            JsonSerializer.Serialize(eventInfo),
            GetCacheTimeToLive(cacheOptions.EventTimeToLiveMinutes));
}
