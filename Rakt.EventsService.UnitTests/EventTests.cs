using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Application;
using Rakt.EventsService.Domain;
using Rakt.EventsService.Domain.Exceptions;
using Rakt.EventsService.Infrastructure;
using Xunit;

namespace Rakt.EventsService.UnitTests;

/// <summary>Изолированные проверки доменной модели и CRUD-сценариев событий.</summary>
[Trait("Category", "Unit")]
public sealed class EventTests
{
    /// <summary>Проверяет создание события.</summary>
    [Fact]
    public async Task Create_ShouldCreateEvent()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var dto = CreateEventDto("Встреча команды", 10);

        var result = await service.CreateAsync(dto);

        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(dto.TotalSeats, result.AvailableSeats);
        Assert.False(result.IsFull);
    }

    /// <summary>Проверяет запрет нулевой и отрицательной вместимости.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldThrowInvalidTotalSeatsException_WhenTotalSeatsIsNotPositive(int seats)
    {
        Assert.Throws<InvalidTotalSeatsException>(() => Event.Create("Событие", null, Utc(1), Utc(2), seats));
    }

    /// <summary>Проверяет резервирование и возврат мест.</summary>
    [Fact]
    public void TryReserveSeats_ShouldReserveAndReleaseSeats()
    {
        var entity = Event.Create("Событие", null, Utc(1), Utc(2), 2);

        Assert.True(entity.TryReserveSeats());
        Assert.True(entity.TryReserveSeats());
        Assert.True(entity.IsFull);
        Assert.False(entity.TryReserveSeats());

        entity.ReleaseSeats();

        Assert.Equal(1, entity.AvailableSeats);
        Assert.False(entity.IsFull);
    }

    /// <summary>Проверяет возврат всех созданных событий.</summary>
    [Fact]
    public async Task GetAll_ShouldReturnAllEvents()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.CreateAsync(CreateEventDto("Событие 1", 10));
        await service.CreateAsync(CreateEventDto("Событие 2", 10, Utc(3)));

        var result = await service.GetAllAsync(new EventQueryDto());

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.CurrentCount);
    }

    /// <summary>Проверяет получение события по идентификатору.</summary>
    [Fact]
    public async Task GetById_ShouldReturnEvent_WhenEventExists()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateEventDto("Найти меня", 10));

        var result = await service.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Найти меня", result.Title);
    }

    /// <summary>Проверяет обновление события.</summary>
    [Fact]
    public async Task Update_ShouldUpdateEvent_WhenEventExists()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateEventDto("Старый", 10));
        var dto = new UpdateEventDto { Title = "Новый", Description = "Описание", StartAt = Utc(4), EndAt = Utc(5) };

        await service.UpdateAsync(created.Id, dto);
        var updated = await service.GetByIdAsync(created.Id);

        Assert.Equal(dto.Title, updated.Title);
        Assert.Equal(dto.Description, updated.Description);
        Assert.Equal(dto.StartAt, updated.StartAt);
    }

    /// <summary>Проверяет сохранение обновления в независимом контексте.</summary>
    [Fact]
    public async Task Update_ShouldPersistChangesAcrossContexts()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var firstContext = CreateContext(databaseName);
        var service = CreateService(firstContext);
        var created = await service.CreateAsync(CreateEventDto("Исходный", 10));
        var dto = new UpdateEventDto
        {
            Title = "Обновлённый",
            Description = "Новое описание",
            StartAt = Utc(4),
            EndAt = Utc(5)
        };

        await service.UpdateAsync(created.Id, dto);

        await using var verificationContext = CreateContext(databaseName);
        var updated = await CreateService(verificationContext).GetByIdAsync(created.Id);

        Assert.Equal(dto.Title, updated.Title);
        Assert.Equal(dto.Description, updated.Description);
    }

    /// <summary>Проверяет удаление события.</summary>
    [Fact]
    public async Task Delete_ShouldRemoveEvent_WhenEventExists()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateEventDto("Удаляемое", 10));

        await service.DeleteAsync(created.Id);
        var result = await service.GetAllAsync(new EventQueryDto());

        Assert.Empty(result.Items);
    }

    /// <summary>Проверяет фильтрацию по диапазону дат.</summary>
    [Fact]
    public async Task GetAll_ShouldFilterByDates()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.CreateAsync(CreateEventDto("Раннее", 10, Utc(1)));
        await service.CreateAsync(CreateEventDto("Подходящее", 10, Utc(10)));
        await service.CreateAsync(CreateEventDto("Позднее", 10, Utc(20)));

        var result = await service.GetAllAsync(new EventQueryDto { From = Utc(5), To = Utc(15).AddHours(1) });

        Assert.Single(result.Items);
        Assert.Equal("Подходящее", result.Items.Single().Title);
    }

    /// <summary>Проверяет пагинацию.</summary>
    [Fact]
    public async Task GetAll_ShouldApplyPagination()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        for (var index = 1; index <= 5; index++)
        {
            await service.CreateAsync(CreateEventDto($"Событие {index}", 10, Utc(index)));
        }

        var result = await service.GetAllAsync(new EventQueryDto { Page = 2, PageSize = 2 });

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(["Событие 3", "Событие 4"], result.Items.Select(item => item.Title));
    }

    /// <summary>Проверяет исключение для отсутствующего события.</summary>
    [Fact]
    public async Task GetById_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        await using var context = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService(context).GetByIdAsync(Guid.NewGuid()));
    }

    /// <summary>Проверяет исключение при обновлении отсутствующего события.</summary>
    [Fact]
    public async Task Update_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        await using var context = CreateContext();
        var dto = new UpdateEventDto
        {
            Title = "Обновление",
            StartAt = Utc(1),
            EndAt = Utc(2)
        };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService(context).UpdateAsync(Guid.NewGuid(), dto));
    }

    /// <summary>Проверяет отмену операции токеном.</summary>
    [Fact]
    public async Task CreateAsync_ShouldThrowOperationCanceled_WhenTokenIsCancelled()
    {
        await using var context = CreateContext();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateService(context).CreateAsync(CreateEventDto("Отмена", 10), source.Token));
    }

    /// <summary>Проверяет DataAnnotations DTO создания.</summary>
    [Fact]
    public void CreateEventDto_ShouldBeInvalid_WhenRequiredFieldsAreMissing()
    {
        var results = Validate(new CreateEventDto());
        Assert.Contains(results, result => result.ErrorMessage!.Contains("Заголовок"));
        Assert.Contains(results, result => result.ErrorMessage!.Contains("Дата начала"));
        Assert.Contains(results, result => result.ErrorMessage!.Contains("Дата окончания"));
        Assert.Contains(results, result => result.ErrorMessage!.Contains("Количество мест"));
    }

    /// <summary>Проверяет DataAnnotations DTO обновления.</summary>
    [Fact]
    public void UpdateEventDto_ShouldBeInvalid_WhenEndAtEarlierThanStartAt()
    {
        var results = Validate(new UpdateEventDto { Title = "Событие", StartAt = Utc(2), EndAt = Utc(1) });
        Assert.Contains(results, result => result.ErrorMessage == "Дата окончания должна быть больше даты начала");
    }

    /// <summary>Проверяет DataAnnotations query DTO.</summary>
    [Fact]
    public void EventQueryDto_ShouldBeInvalid_WhenFromLaterThanTo()
    {
        var results = Validate(new EventQueryDto { From = Utc(2), To = Utc(1) });
        Assert.Contains(results, result => result.ErrorMessage == "Дата начала не может быть позже даты окончания");
    }

    private static EventsDbContext CreateContext()
    {
        return CreateContext(Guid.NewGuid().ToString());
    }

    private static EventsDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new EventsDbContext(options);
    }

    private static IEventService CreateService(EventsDbContext context) => new EventService(new EventRepository(context), new TestCache());

    private static CreateEventDto CreateEventDto(string title, int seats, DateTimeOffset? startAt = null) => new() { Title = title, TotalSeats = seats, StartAt = startAt ?? Utc(1), EndAt = (startAt ?? Utc(1)).AddHours(1) };

    private static DateTimeOffset Utc(int day) => new(2026, 4, day, 10, 0, 0, TimeSpan.Zero);

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }
}
