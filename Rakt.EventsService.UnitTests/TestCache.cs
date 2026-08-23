using Rakt.EventsService.Application;

namespace Rakt.EventsService.UnitTests;

/// <summary>
/// Предоставляет потокобезопасную in-memory реализацию кеша для unit-тестов.
/// </summary>
internal sealed class TestCache : ICache
{
    private readonly Dictionary<string, string> values = [];

    /// <inheritdoc />
    public Task<string?> GetAsync(string key)
    {
        return Task.FromResult(values.GetValueOrDefault(key));
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, TimeSpan timeToLive)
    {
        values[key] = value;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        values.Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Помещает значение в кеш до выполнения тестируемого сценария.
    /// </summary>
    /// <param name="key">Ключ сохраняемого значения.</param>
    /// <param name="value">Сохраняемое значение.</param>
    public void Seed(string key, string value)
    {
        values[key] = value;
    }

    /// <summary>
    /// Возвращает значение, записанное тестируемым сценарием.
    /// </summary>
    /// <param name="key">Ключ кеша.</param>
    /// <returns>Значение или <see langword="null"/>, если ключ отсутствует.</returns>
    public string? GetValue(string key) => values.GetValueOrDefault(key);
}
