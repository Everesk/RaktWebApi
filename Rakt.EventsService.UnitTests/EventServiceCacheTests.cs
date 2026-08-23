using Rakt.EventsService.Application;
using Rakt.EventsService.Domain;
using System.Text.Json;
using Xunit;

namespace Rakt.EventsService.UnitTests;

/// <summary>
/// Проверяет Cache-Aside сценарии сервиса событий с изолированными заглушками зависимостей.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EventServiceCacheTests
{
    /// <summary>
    /// При наличии значения в кеше сервис не обращается к репозиторию.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_DoesNotCallRepository_WhenCacheContainsEvent()
    {
        var eventId = Guid.NewGuid();
        var cache = new TestCache();
        cache.Seed(
            $"event:{eventId}",
            JsonSerializer.Serialize(new EventInfoDto
            {
                Id = eventId,
                Title = "Кешированное событие",
                StartAt = DateTimeOffset.UtcNow.AddDays(1),
                EndAt = DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
                TotalSeats = 10,
                AvailableSeats = 10
            }));
        var repository = new StubEventRepository();
        var service = CreateService(repository, cache);

        var result = await service.GetByIdAsync(eventId);

        Assert.Equal("Кешированное событие", result.Title);
        Assert.Equal(0, repository.GetAsyncCallCount);
    }

    /// <summary>
    /// При промахе кеша сервис загружает событие из репозитория и сохраняет его в кеше.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_LoadsAndCachesEvent_WhenCacheMisses()
    {
        var entity = CreateEvent("Событие из репозитория");
        var cache = new TestCache();
        var repository = new StubEventRepository { Event = entity };
        var service = CreateService(repository, cache);

        var result = await service.GetByIdAsync(entity.Id);
        var cachedEvent = JsonSerializer.Deserialize<EventInfoDto>(cache.GetValue($"event:{entity.Id}")!);

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(1, repository.GetAsyncCallCount);
        Assert.Equal(entity.Id, cachedEvent!.Id);
        Assert.Equal(entity.Title, cachedEvent.Title);
    }

    /// <summary>
    /// Создание и обновление перезаписывают кеш, а удаление удаляет ключ после сохранения в репозитории.
    /// </summary>
    [Fact]
    public async Task WriteOperations_UpdateAndRemoveCache_AfterRepositorySave()
    {
        var cache = new TestCache();
        var repository = new StubEventRepository();
        var service = CreateService(repository, cache);
        var startAt = DateTimeOffset.UtcNow.AddDays(1);

        var created = await service.CreateAsync(new CreateEventDto
        {
            Title = "Исходное",
            StartAt = startAt,
            EndAt = startAt.AddHours(1),
            TotalSeats = 10
        });
        var cachedAfterCreate = JsonSerializer.Deserialize<EventInfoDto>(cache.GetValue($"event:{created.Id}")!);

        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.Equal("Исходное", cachedAfterCreate!.Title);

        await service.UpdateAsync(
            created.Id,
            new UpdateEventDto
            {
                Title = "Обновлённое",
                StartAt = startAt.AddDays(1),
                EndAt = startAt.AddDays(1).AddHours(1)
            });
        var cachedAfterUpdate = JsonSerializer.Deserialize<EventInfoDto>(cache.GetValue($"event:{created.Id}")!);

        Assert.Equal(2, repository.SaveChangesCallCount);
        Assert.Equal("Обновлённое", cachedAfterUpdate!.Title);

        await service.DeleteAsync(created.Id);

        Assert.Equal(3, repository.SaveChangesCallCount);
        Assert.Null(cache.GetValue($"event:{created.Id}"));
    }

    /// <summary>
    /// Создаёт сервис с заглушками кеша и репозитория.
    /// </summary>
    private static EventService CreateService(StubEventRepository repository, TestCache cache) =>
        new(repository, cache, new CacheOptions());

    /// <summary>
    /// Создаёт событие для заглушки репозитория.
    /// </summary>
    private static Event CreateEvent(string title)
    {
        var startAt = DateTimeOffset.UtcNow.AddDays(1);
        return Event.Create(title, null, startAt, startAt.AddHours(1), 10);
    }

    /// <summary>
    /// Предоставляет управляемую заглушку порта хранения событий.
    /// </summary>
    private sealed class StubEventRepository : IEventRepository
    {
        /// <summary>Событие, возвращаемое заглушкой.</summary>
        public Event? Event { get; set; }

        /// <summary>Количество вызовов получения события для чтения.</summary>
        public int GetAsyncCallCount { get; private set; }

        /// <summary>Количество вызовов сохранения изменений.</summary>
        public int SaveChangesCallCount { get; private set; }

        /// <inheritdoc />
        public Task<PaginatedResult<Event>> GetAllAsync(EventQueryDto query, CancellationToken ct = default) =>
            Task.FromResult(new PaginatedResult<Event> { Items = Event is null ? [] : [Event] });

        /// <inheritdoc />
        public Task<Event?> GetAsync(Guid id, CancellationToken ct = default)
        {
            GetAsyncCallCount++;
            return Task.FromResult(Event?.Id == id ? Event : null);
        }

        /// <inheritdoc />
        public Task<Event?> GetForUpdateAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Event?.Id == id ? Event : null);

        /// <inheritdoc />
        public Task<IReadOnlyList<Event>> GetTopAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Event>>(Event is null ? [] : [Event]);

        /// <inheritdoc />
        public Task<BookingSeatReservation?> GetReservationAsync(Guid bookingId, CancellationToken ct = default) =>
            Task.FromResult<BookingSeatReservation?>(null);

        /// <inheritdoc />
        public Task AddAsync(Event entity, CancellationToken ct = default)
        {
            Event = entity;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task AddReservationAsync(BookingSeatReservation reservation, CancellationToken ct = default) => Task.CompletedTask;

        /// <inheritdoc />
        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteAsync(Event entity, CancellationToken ct = default)
        {
            Event = null;
            return Task.CompletedTask;
        }
    }
}
