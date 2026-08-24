namespace Rakt.EventsService.Application;

/// <summary>
/// Формирует ключи кеша, используемые сценариями сервиса событий.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Возвращает ключ кеша для отдельного события.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <returns>Ключ кеша события.</returns>
    public static string SingleEvent(Guid id) => $"event:{id}";

    /// <summary>
    /// Возвращает ключ кеша рейтинга десяти самых популярных событий.
    /// </summary>
    /// <returns>Ключ кеша рейтинга событий.</returns>
    public static string TopEvents() => "events:top10";
}
