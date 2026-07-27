using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Application;
using Rakt.EventsService.Domain;
using Rakt.EventsService.Infrastructure;

namespace Rakt.EventsService.IntegrationTests;

/// <summary>
/// Интеграционные тесты сервиса событий.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EventsServiceIntegrationTests(EventsPostgreSqlFixture fixture)
    : IClassFixture<EventsPostgreSqlFixture>, IAsyncLifetime
{
    /// <summary>
    /// Подготавливает чистую базу событий.
    /// </summary>
    public Task InitializeAsync()
    {
        return fixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// Не требует действий после теста.
    /// </summary>
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Проверяет применение обеих миграций и состав таблицы событий.
    /// </summary>
    [Fact]
    public async Task Migrations_CreateEventsTableWithDetailsColumns()
    {
        await using var context = fixture.CreateDbContext();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        var columns = await context.Database
            .SqlQueryRaw<string>("SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_name = 'events'")
            .ToListAsync();

        Assert.Equal(2, appliedMigrations.Count());
        Assert.Contains("Description", columns);
        Assert.Contains("EndAt", columns);
        Assert.Contains("AvailableSeats", columns);
    }

    /// <summary>
    /// Проверяет сохранение события и его получение через фильтр репозитория.
    /// </summary>
    [Fact]
    public async Task Repository_PersistsAndQueriesEvent()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new EventRepository(context);
        var eventEntity = Event.Create(
            "Интеграционное событие",
            "Описание",
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            totalSeats: 10);

        await repository.AddAsync(eventEntity);
        await repository.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var result = await repository.GetAllAsync(new EventQueryDto
        {
            Title = "Интеграционное"
        });

        Assert.Single(result.Items);
        Assert.Equal(eventEntity.Id, result.Items.Single().Id);
        Assert.Equal(10, result.Items.Single().AvailableSeats);
    }
}
