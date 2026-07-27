using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Infrastructure;
using Testcontainers.PostgreSql;

namespace Rakt.EventsService.IntegrationTests;

/// <summary>
/// Управляет контейнером PostgreSQL для интеграционных тестов событий.
/// </summary>
public sealed class EventsPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("rakt_events_integration_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    /// <summary>
    /// Запускает контейнер PostgreSQL.
    /// </summary>
    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    /// <summary>
    /// Удаляет содержимое базы и применяет миграции сервиса событий.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Создаёт контекст данных событий.
    /// </summary>
    public EventsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new EventsDbContext(options);
    }

    /// <summary>
    /// Останавливает и удаляет контейнер PostgreSQL.
    /// </summary>
    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
