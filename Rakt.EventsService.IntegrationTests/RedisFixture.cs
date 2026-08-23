using StackExchange.Redis;
using Testcontainers.Redis;

namespace Rakt.EventsService.IntegrationTests;

/// <summary>
/// Управляет контейнером Redis для интеграционных тестов событий.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer container = new RedisBuilder()
        .WithImage("redis:7.2-alpine")
        .Build();

    /// <summary>
    /// Запускает контейнер Redis.
    /// </summary>
    public Task InitializeAsync()
    {
        return container.StartAsync();
    }

    /// <summary>
    /// Создаёт подключение к Redis контейнера.
    /// </summary>
    /// <returns>Потокобезопасное подключение к Redis.</returns>
    public IConnectionMultiplexer CreateConnection()
    {
        return ConnectionMultiplexer.Connect(container.GetConnectionString());
    }

    /// <summary>
    /// Останавливает и удаляет контейнер Redis.
    /// </summary>
    public Task DisposeAsync()
    {
        return container.DisposeAsync().AsTask();
    }
}
