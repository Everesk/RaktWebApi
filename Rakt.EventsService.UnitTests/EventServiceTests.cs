using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Application;
using Rakt.EventsService.Domain;
using Rakt.EventsService.Domain.Exceptions;
using Rakt.EventsService.Infrastructure;
using System.Text.Json;
using Xunit;

namespace Rakt.EventsService.UnitTests;

/// <summary>
/// Проверки прикладных CRUD-сценариев событий.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EventServiceTests
{
    /// <summary>
    /// Создание события сохраняет его и возвращает доступные места.
    /// </summary>
    [Fact]
    public async Task CreateAsync_SavesEvent()
    {
        await using var context = CreateContext();
        var service = new EventService(new EventRepository(context), new TestCache());

        var result = await service.CreateAsync(CreateCommand("Концерт", totalSeats: 3));

        Assert.Equal("Концерт", result.Title);
        Assert.Equal(3, result.AvailableSeats);
        Assert.Single(context.Events);
    }

    /// <summary>
    /// Фильтрация и пагинация возвращают нужную страницу событий.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_AppliesTitleFilterAndPagination()
    {
        await using var context = CreateContext();
        var service = new EventService(new EventRepository(context), new TestCache());
        await service.CreateAsync(CreateCommand("Встреча 1"));
        await service.CreateAsync(CreateCommand("Встреча 2"));
        await service.CreateAsync(CreateCommand("Другое"));

        var result = await service.GetAllAsync(new EventQueryDto
        {
            Title = "Встреча",
            Page = 2,
            PageSize = 1
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Встреча 2", result.Items.Single().Title);
    }

    /// <summary>
    /// Обновление изменяет сохранённое событие.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_UpdatesExistingEvent()
    {
        await using var context = CreateContext();
        var service = new EventService(new EventRepository(context), new TestCache());
        var created = await service.CreateAsync(CreateCommand("Старое"));

        await service.UpdateAsync(
            created.Id,
            new UpdateEventDto
            {
                Title = "Новое",
                Description = "Описание",
                StartAt = DateTimeOffset.UtcNow.AddDays(2),
                EndAt = DateTimeOffset.UtcNow.AddDays(2).AddHours(1)
            });

        var updated = await service.GetByIdAsync(created.Id);
        Assert.Equal("Новое", updated.Title);
        Assert.Equal("Описание", updated.Description);
    }

    /// <summary>
    /// Запрос отсутствующего события возвращает доменное исключение.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ThrowsWhenEventDoesNotExist()
    {
        await using var context = CreateContext();
        var service = new EventService(new EventRepository(context), new TestCache());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Получение события возвращает значение из кеша без обращения к хранилищу.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsCachedEvent_WhenCacheContainsValue()
    {
        await using var context = CreateContext();
        var cachedEvent = new EventInfoDto
        {
            Id = Guid.NewGuid(),
            Title = "Из кеша",
            StartAt = DateTimeOffset.UtcNow.AddDays(1),
            EndAt = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            TotalSeats = 10,
            AvailableSeats = 5
        };
        var cache = new TestCache();
        cache.Seed($"event:{cachedEvent.Id}", JsonSerializer.Serialize(cachedEvent));
        var service = new EventService(new EventRepository(context), cache);

        var result = await service.GetByIdAsync(cachedEvent.Id);

        Assert.Equal(cachedEvent.Id, result.Id);
        Assert.Equal("Из кеша", result.Title);
    }

    /// <summary>
    /// При промахе кеша топ событий загружается из БД и сохраняется в кеше.
    /// </summary>
    [Fact]
    public async Task GetTopAsync_LoadsFromRepositoryAndCachesResult_WhenCacheMisses()
    {
        await using var context = CreateContext();
        var mostPopular = Event.Create("Популярное", null, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(1), 10);
        mostPopular.TryReserveSeats(8);
        var lessPopular = Event.Create("Менее популярное", null, DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(2).AddHours(1), 10);
        lessPopular.TryReserveSeats(3);
        context.Events.AddRange(mostPopular, lessPopular);
        await context.SaveChangesAsync();
        var cache = new TestCache();
        var service = new EventService(new EventRepository(context), cache);

        var result = await service.GetTopAsync();

        Assert.Equal(["Популярное", "Менее популярное"], result.Select(item => item.Title));
        Assert.NotNull(cache.GetValue("events:top10"));
    }

    /// <summary>
    /// Создаёт команду события с корректным временным интервалом.
    /// </summary>
    private static CreateEventDto CreateCommand(string title, int totalSeats = 1)
    {
        var startAt = DateTimeOffset.UtcNow.AddDays(1);

        return new CreateEventDto
        {
            Title = title,
            StartAt = startAt,
            EndAt = startAt.AddHours(1),
            TotalSeats = totalSeats
        };
    }

    /// <summary>
    /// Создаёт изолированное хранилище событий в памяти.
    /// </summary>
    private static EventsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EventsDbContext(options);
    }
}
