using RaktApi.Domain;
using RaktWebApi.Models.DTO;

namespace Rakt.IntegrationTests;

/// <summary>
/// Интеграционные тесты репозитория событий на PostgreSQL в Testcontainers.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
[Trait("Category", "Integration")]
public sealed class EventRepositoryTests(PostgreSqlFixture fixture) : PostgreSqlTestBase(fixture)
{
    /// <summary>Проверяет добавление и получение события по идентификатору.</summary>
    [Fact]
    public async Task AddAsync_AndGetByIdAsync_PersistsEvent()
    {
        await using var repositories = CreateRepositories();
        var eventEntity = CreateEvent("Dotnet Conf", 10, 10);

        await repositories.Events.AddAsync(eventEntity);
        var storedEvent = await repositories.Events.GetByIdAsync(eventEntity.Id);

        Assert.NotNull(storedEvent);
        Assert.Equal("Dotnet Conf", storedEvent.Title);
    }

    /// <summary>Проверяет возврат null при отсутствии события.</summary>
    [Fact]
    public async Task GetByIdAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        await using var repositories = CreateRepositories();

        var storedEvent = await repositories.Events.GetByIdAsync(Guid.NewGuid());

        Assert.Null(storedEvent);
    }

    /// <summary>Проверяет сохранение изменений отслеживаемого события.</summary>
    [Fact]
    public async Task GetForUpdateAsync_AndUpdateAsync_PersistsChanges()
    {
        await using var repositories = CreateRepositories();
        var eventEntity = CreateEvent("До обновления", 10, 10);
        await repositories.Events.AddAsync(eventEntity);

        var trackedEvent = await repositories.Events.GetForUpdateAsync(eventEntity.Id);
        Assert.NotNull(trackedEvent);
        trackedEvent.Update("После обновления", "Описание", Date(12), Date(13));
        await repositories.Events.UpdateAsync();

        var storedEvent = await repositories.Events.GetByIdAsync(eventEntity.Id);
        Assert.Equal("После обновления", storedEvent!.Title);
        Assert.Equal("Описание", storedEvent.Description);
    }

    /// <summary>Проверяет возврат null при изменении отсутствующего события.</summary>
    [Fact]
    public async Task GetForUpdateAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        await using var repositories = CreateRepositories();

        var eventEntity = await repositories.Events.GetForUpdateAsync(Guid.NewGuid());

        Assert.Null(eventEntity);
    }

    /// <summary>Проверяет удаление события.</summary>
    [Fact]
    public async Task DeleteAsync_RemovesEvent()
    {
        await using var repositories = CreateRepositories();
        var eventEntity = CreateEvent("Удаляемое", 10, 10);
        await repositories.Events.AddAsync(eventEntity);

        await repositories.Events.DeleteAsync(eventEntity);

        Assert.Null(await repositories.Events.GetByIdAsync(eventEntity.Id));
    }

    /// <summary>Проверяет выборку без фильтров и сортировку по дате начала.</summary>
    [Fact]
    public async Task GetAllAsync_WithoutFilters_ReturnsAllEventsInOrder()
    {
        await using var repositories = CreateRepositories();
        await AddEventsAsync(repositories.Events,
            CreateEvent("Позднее", 14, 15),
            CreateEvent("Раньше", 10, 11));

        var result = await repositories.Events.GetAllAsync(new EventQueryDto());

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(["Раньше", "Позднее"], result.Items.Select(item => item.Title));
        Assert.Equal(2, result.CurrentCount);
    }

    /// <summary>Проверяет регистронезависимый фильтр по части заголовка.</summary>
    [Fact]
    public async Task GetAllAsync_WithTitleFilter_ReturnsMatchingEvents()
    {
        await using var repositories = CreateRepositories();
        await AddEventsAsync(repositories.Events,
            CreateEvent("DotNet Meetup", 10, 11),
            CreateEvent("Java Meetup", 12, 13));

        var result = await repositories.Events.GetAllAsync(new EventQueryDto { Title = "dotnet" });

        Assert.Single(result.Items);
        Assert.Equal("DotNet Meetup", result.Items.Single().Title);
    }

    /// <summary>Проверяет фильтр по минимальному времени начала.</summary>
    [Fact]
    public async Task GetAllAsync_WithFromFilter_ReturnsEventsStartingNotEarlier()
    {
        await using var repositories = CreateRepositories();
        await AddEventsAsync(repositories.Events, CreateEvent("До", 10, 11), CreateEvent("После", 12, 13));

        var result = await repositories.Events.GetAllAsync(new EventQueryDto { From = Date(12) });

        Assert.Equal(["После"], result.Items.Select(item => item.Title));
    }

    /// <summary>Проверяет фильтр по максимальному времени окончания.</summary>
    [Fact]
    public async Task GetAllAsync_WithToFilter_ReturnsEventsEndingNotLater()
    {
        await using var repositories = CreateRepositories();
        await AddEventsAsync(repositories.Events, CreateEvent("Подходит", 10, 11), CreateEvent("Поздно", 12, 13));

        var result = await repositories.Events.GetAllAsync(new EventQueryDto { To = Date(11) });

        Assert.Equal(["Подходит"], result.Items.Select(item => item.Title));
    }

    /// <summary>Проверяет совместное применение фильтров и пагинации.</summary>
    [Fact]
    public async Task GetAllAsync_WithCombinedFiltersAndPagination_ReturnsRequestedPage()
    {
        await using var repositories = CreateRepositories();
        await AddEventsAsync(repositories.Events,
            CreateEvent("Семинар 1", 10, 11),
            CreateEvent("Семинар 2", 12, 13),
            CreateEvent("Семинар 3", 14, 15),
            CreateEvent("Другое", 16, 17));

        var result = await repositories.Events.GetAllAsync(new EventQueryDto
        {
            Title = "семинар",
            From = Date(12),
            To = Date(15),
            Page = 2,
            PageSize = 1
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal("Семинар 3", result.Items.Single().Title);
    }

    /// <summary>Создаёт событие с заданным диапазоном времени.</summary>
    private static Event CreateEvent(string title, int startHour, int endHour) => Event.Create(title, null, Date(startHour), Date(endHour), 10);

    /// <summary>Возвращает фиксированное время тестового события.</summary>
    private static DateTimeOffset Date(int hour) => new(2026, 1, 1, hour, 0, 0, TimeSpan.Zero);

    /// <summary>Сохраняет набор событий в репозитории.</summary>
    private static async Task AddEventsAsync(RaktWebApi.Repositories.EventRepository repository, params Event[] events)
    {
        foreach (var eventEntity in events)
        {
            await repository.AddAsync(eventEntity);
        }
    }
}
