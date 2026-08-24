using Rakt.EventsService.Application;
using Rakt.EventsService.Domain;
using System.Text.Json;

namespace Rakt.EventsService.Infrastructure;

/// <summary>
/// Обновляет кеш события после изменения доменной модели вне прикладного сервиса.
/// </summary>
internal static class EventCacheUpdater
{
    /// <summary>
    /// Сохраняет актуальные данные события в кеше с настроенным временем жизни.
    /// </summary>
    /// <param name="cache">Кеш приложения.</param>
    /// <param name="cacheOptions">Настройки времени жизни кеша.</param>
    /// <param name="eventEntity">Изменённое событие.</param>
    internal static Task UpdateAsync(ICache cache, CacheOptions cacheOptions, Event eventEntity)
    {
        return cache.SetAsync(
            CacheKeys.SingleEvent(eventEntity.Id),
            JsonSerializer.Serialize(EventInfoDto.FromEntity(eventEntity)),
            TimeSpan.FromMinutes(cacheOptions.EventTimeToLiveMinutes));
    }
}
