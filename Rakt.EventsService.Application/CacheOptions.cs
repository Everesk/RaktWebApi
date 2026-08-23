using System.ComponentModel.DataAnnotations;

namespace Rakt.EventsService.Application;

/// <summary>
/// Настройки времени жизни значений в кеше.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Имя секции конфигурации с настройками кеша.
    /// </summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// Количество минут, в течение которых в кеше хранится отдельное событие.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int EventTimeToLiveMinutes { get; init; } = 5;

    /// <summary>
    /// Количество минут, в течение которых в кеше хранится рейтинг популярных событий.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int TopEventsTimeToLiveMinutes { get; init; } = 5;
}
