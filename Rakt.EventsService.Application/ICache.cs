namespace Rakt.EventsService.Application;

/// <summary>
/// Определяет операции кеширования данных приложения.
/// </summary>
public interface ICache
{
    /// <summary>
    /// Возвращает значение, сохраненное по указанному ключу.
    /// </summary>
    /// <param name="key">Уникальный ключ кеша.</param>
    /// <returns>Сохраненное значение или <see langword="null"/>, если ключ отсутствует либо кеш недоступен.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Сохраняет значение по указанному ключу на ограниченное время.
    /// </summary>
    /// <param name="key">Уникальный ключ кеша.</param>
    /// <param name="value">Сохраняемое значение.</param>
    /// <param name="timeToLive">Время жизни значения в кеше.</param>
    Task SetAsync(string key, string value, TimeSpan timeToLive);

    /// <summary>
    /// Удаляет значение по указанному ключу из кеша.
    /// </summary>
    /// <param name="key">Уникальный ключ кеша.</param>
    Task RemoveAsync(string key);
}
