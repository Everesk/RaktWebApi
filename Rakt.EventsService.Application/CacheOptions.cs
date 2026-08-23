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
    /// Количество минут, в течение которых значение хранится в кеше.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int TimeToLiveMinutes { get; init; } = 5;
}
