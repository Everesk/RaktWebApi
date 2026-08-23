using Microsoft.Extensions.Logging.Abstractions;
using Rakt.EventsService.Application;
using Rakt.EventsService.Domain;
using Rakt.EventsService.Infrastructure;
using StackExchange.Redis;
using System.Text.Json;

namespace Rakt.EventsService.IntegrationTests;

/// <summary>
/// Интеграционные проверки Redis-кеша сервиса событий.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EventsRedisIntegrationTests(
    EventsPostgreSqlFixture postgreSqlFixture,
    RedisFixture redisFixture) :
    IClassFixture<EventsPostgreSqlFixture>,
    IClassFixture<RedisFixture>,
    IAsyncLifetime
{
    /// <summary>
    /// Подготавливает чистую базу данных перед каждым тестом.
    /// </summary>
    public Task InitializeAsync()
    {
        return postgreSqlFixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// Не требует действий после каждого теста.
    /// </summary>
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Проверяет, что Redis-адаптер записывает, извлекает и удаляет значения в реальном Redis.
    /// </summary>
    [Fact]
    public async Task RedisCache_WritesReadsAndRemovesValue()
    {
        using var connection = redisFixture.CreateConnection();
        var cache = new RedisCache(connection, NullLogger<RedisCache>.Instance);
        var key = $"integration:cache:{Guid.NewGuid()}";

        await cache.SetAsync(key, "значение", TimeSpan.FromMinutes(1));

        Assert.Equal("значение", await cache.GetAsync(key));

        await cache.RemoveAsync(key);

        Assert.Null(await cache.GetAsync(key));
    }

    /// <summary>
    /// Проверяет обновление Redis после изменения события, сохранённого в PostgreSQL.
    /// </summary>
    [Fact]
    public async Task EventService_UpdatesRedisCache_AfterEventUpdate()
    {
        using var connection = redisFixture.CreateConnection();
        await using var context = postgreSqlFixture.CreateDbContext();
        var cache = new RedisCache(connection, NullLogger<RedisCache>.Instance);
        var service = new EventService(new EventRepository(context), cache, new CacheOptions());
        var startAt = DateTimeOffset.UtcNow.AddDays(1);
        var created = await service.CreateAsync(new CreateEventDto
        {
            Title = "Исходное событие",
            StartAt = startAt,
            EndAt = startAt.AddHours(1),
            TotalSeats = 10
        });

        await service.UpdateAsync(
            created.Id,
            new UpdateEventDto
            {
                Title = "Обновлённое событие",
                StartAt = startAt.AddDays(1),
                EndAt = startAt.AddDays(1).AddHours(1)
            });
        var cachedValue = await cache.GetAsync($"event:{created.Id}");
        var cachedEvent = JsonSerializer.Deserialize<EventInfoDto>(cachedValue!);

        Assert.Equal("Обновлённое событие", cachedEvent!.Title);
    }

    /// <summary>
    /// Проверяет получение события из PostgreSQL, когда Redis недоступен.
    /// </summary>
    [Fact]
    public async Task EventService_LoadsEventFromDatabase_WhenRedisIsUnavailable()
    {
        await using var context = postgreSqlFixture.CreateDbContext();
        var entity = CreateEvent("Событие из PostgreSQL");
        context.Events.Add(entity);
        await context.SaveChangesAsync();
        using var unavailableConnection = CreateUnavailableRedisConnection();
        var cache = new RedisCache(unavailableConnection, NullLogger<RedisCache>.Instance);
        var service = new EventService(new EventRepository(context), cache, new CacheOptions());

        var result = await service.GetByIdAsync(entity.Id);

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("Событие из PostgreSQL", result.Title);
    }

    /// <summary>
    /// Создаёт событие для проверки чтения из PostgreSQL.
    /// </summary>
    private static Event CreateEvent(string title)
    {
        var startAt = DateTimeOffset.UtcNow.AddDays(1);
        return Event.Create(title, null, startAt, startAt.AddHours(1), 10);
    }

    /// <summary>
    /// Создаёт подключение к несуществующему Redis, не завершая приложение ошибкой.
    /// </summary>
    private static IConnectionMultiplexer CreateUnavailableRedisConnection()
    {
        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectRetry = 0,
            ConnectTimeout = 100,
            SyncTimeout = 100
        };
        options.EndPoints.Add("127.0.0.1", 1);

        return ConnectionMultiplexer.Connect(options);
    }
}
