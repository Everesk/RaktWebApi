using Microsoft.Extensions.Logging;
using Rakt.EventsService.Application;
using StackExchange.Redis;

namespace Rakt.EventsService.Infrastructure;

/// <summary>
/// Реализует кеш приложения с использованием Redis.
/// </summary>
public sealed class RedisCache(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCache> logger) : ICache
{
    /// <inheritdoc />
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            var value = await connectionMultiplexer.GetDatabase().StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            logger.LogWarning(exception, "Не удалось получить значение из Redis по ключу {CacheKey}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, string value, TimeSpan timeToLive)
    {
        try
        {
            await connectionMultiplexer.GetDatabase().StringSetAsync(key, value, timeToLive);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            logger.LogWarning(exception, "Не удалось сохранить значение в Redis по ключу {CacheKey}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key)
    {
        try
        {
            await connectionMultiplexer.GetDatabase().KeyDeleteAsync(key);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            logger.LogWarning(exception, "Не удалось удалить значение из Redis по ключу {CacheKey}", key);
        }
    }
}
