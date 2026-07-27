using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RaktApi.Domain;
using RaktApi.Domain.Exceptions;
using RaktApi.Application.DTO;
using RaktApi.Application.Ports;
using RaktApi.Application.Services;
using RaktApi.Infrastructure.Repositories;
using Rakt.Tests.Infrastructure;

namespace Rakt.Tests.Services;

/// <summary>
/// Набор тестов для сервиса <see cref="EventService"/>.
/// </summary>
[Trait("Category", "Unit")]
public class EventServiceTests : InMemoryDbTestBase
{
    /// <summary>
    /// Настраивает сервисы, необходимые для тестов <see cref="EventService"/>.
    /// </summary>
    /// <param name="services">Коллекция сервисов DI.</param>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventRepository, EventRepository>();
    }

    /// <summary>
    /// Проверяет, что событие успешно создается.
    /// </summary>
    [Fact]
    public async Task Create_ShouldCreateEvent()
    {
        // Arrange
        var service = CreateService();
        var dto = new CreateEventDto
        {
            Title = "Встреча команды",
            TotalSeats = 10,
            Description = "Обсуждение задач",
            StartAt = Utc(2026, 4, 1, 10, 0, 0),
            EndAt = Utc(2026, 4, 1, 11, 0, 0)
        };

        // Act
        var created = await service.CreateAsync(dto);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.Title.Should().Be(dto.Title);
        created.Description.Should().Be(dto.Description);
        created.StartAt.Should().Be(dto.StartAt);
        created.EndAt.Should().Be(dto.EndAt);
        created.TotalSeats.Should().Be(dto.TotalSeats);
        created.AvailableSeats.Should().Be(dto.TotalSeats);
        created.IsFull.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что событие не создается с некорректным количеством мест.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldThrowInvalidTotalSeatsException_WhenTotalSeatsIsNotPositive(int totalSeats)
    {
        // Act
        Action act = () => Event.Create(
            title: "Некорректное событие",
            description: null,
            startAt: Utc(2026, 4, 1, 10, 0, 0),
            endAt: Utc(2026, 4, 1, 11, 0, 0),
            totalSeats: totalSeats);

        // Assert
        act.Should().Throw<InvalidTotalSeatsException>();
    }

    /// <summary>
    /// Проверяет резервирование и освобождение мест события.
    /// </summary>
    [Fact]
    public void TryReserveSeats_ShouldReserveAndReleaseSeats()
    {
        // Arrange
        var eventEntity = Event.Create(
            title: "Событие с местами",
            description: null,
            startAt: Utc(2026, 4, 1, 10, 0, 0),
            endAt: Utc(2026, 4, 1, 11, 0, 0),
            totalSeats: 2);

        // Act
        var firstReservation = eventEntity.TryReserveSeats();
        var secondReservation = eventEntity.TryReserveSeats();
        var isFullAfterReservations = eventEntity.IsFull;
        var overReservation = eventEntity.TryReserveSeats();
        eventEntity.ReleaseSeats();

        // Assert
        firstReservation.Should().BeTrue();
        secondReservation.Should().BeTrue();
        isFullAfterReservations.Should().BeTrue();
        overReservation.Should().BeFalse();
        eventEntity.AvailableSeats.Should().Be(1);
        eventEntity.IsFull.Should().BeFalse();
    }

    /// <summary>
    /// Проверяет, что сервис возвращает все события.
    /// </summary>
    [Fact]
    public async Task GetAll_ShouldReturnAllEvents()
    {
        // Arrange
        var service = CreateService();

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Событие 1",
            TotalSeats = 10,
            StartAt = Utc(2026, 4, 1, 10, 0, 0),
            EndAt = Utc(2026, 4, 1, 11, 0, 0)
        });

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Событие 2",
            TotalSeats = 10,
            StartAt = Utc(2026, 4, 2, 10, 0, 0),
            EndAt = Utc(2026, 4, 2, 11, 0, 0)
        });

        // Act
        var result = await service.GetAllAsync(new EventQueryDto());

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.CurrentCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    /// <summary>
    /// Проверяет, что можно получить событие по идентификатору.
    /// </summary>
    [Fact]
    public async Task GetById_ShouldReturnEvent_WhenEventExists()
    {
        // Arrange
        var service = CreateService();
        var created = await service.CreateAsync(new CreateEventDto
        {
            Title = "Найти меня",
            TotalSeats = 10,
            StartAt = Utc(2026, 4, 3, 9, 0, 0),
            EndAt = Utc(2026, 4, 3, 10, 0, 0)
        });

        // Act
        var result = await service.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(created.Id);
        result.Title.Should().Be("Найти меня");
    }

    /// <summary>
    /// Проверяет, что существующее событие успешно обновляется.
    /// </summary>
    [Fact]
    public async Task Update_ShouldUpdateEvent_WhenEventExists()
    {
        // Arrange
        var service = CreateService();
        var created = await service.CreateAsync(new CreateEventDto
        {
            Title = "Старый заголовок",
            TotalSeats = 10,
            Description = "Старое описание",
            StartAt = Utc(2026, 4, 4, 10, 0, 0),
            EndAt = Utc(2026, 4, 4, 11, 0, 0)
        });

        var dto = new UpdateEventDto
        {
            Title = "Новый заголовок",
            Description = "Новое описание",
            StartAt = Utc(2026, 4, 4, 12, 0, 0),
            EndAt = Utc(2026, 4, 4, 13, 0, 0)
        };

        // Act
        await service.UpdateAsync(created.Id, dto);
        var updated = await service.GetByIdAsync(created.Id);

        // Assert
        updated.Title.Should().Be("Новый заголовок");
        updated.Description.Should().Be("Новое описание");
        updated.StartAt.Should().Be(dto.StartAt);
        updated.EndAt.Should().Be(dto.EndAt);
    }

    /// <summary>
    /// Проверяет, что обновление проходит через репозиторий, а не зависит от общей ссылки на объект.
    /// </summary>
    [Fact]
    public async Task Update_ShouldPersistChangesAcrossScopes()
    {
        // Arrange
        var service = CreateService();
        var created = await service.CreateAsync(new CreateEventDto
        {
            Title = "Исходный заголовок",
            TotalSeats = 10,
            Description = "Исходное описание",
            StartAt = Utc(2026, 4, 4, 10, 0, 0),
            EndAt = Utc(2026, 4, 4, 11, 0, 0)
        });

        var dto = new UpdateEventDto
        {
            Title = "Обновленный заголовок",
            Description = "Обновленное описание",
            StartAt = Utc(2026, 4, 4, 12, 0, 0),
            EndAt = Utc(2026, 4, 4, 13, 0, 0)
        };

        // Act
        await service.UpdateAsync(created.Id, dto);
        using var verificationScope = CreateScope();
        var verificationService = verificationScope.ServiceProvider.GetRequiredService<IEventService>();
        var updated = await verificationService.GetByIdAsync(created.Id);

        // Assert
        updated.Title.Should().Be(dto.Title);
        updated.Description.Should().Be(dto.Description);
        updated.StartAt.Should().Be(dto.StartAt);
        updated.EndAt.Should().Be(dto.EndAt);
    }

    /// <summary>
    /// Проверяет, что существующее событие успешно удаляется.
    /// </summary>
    [Fact]
    public async Task Delete_ShouldRemoveEvent_WhenEventExists()
    {
        // Arrange
        var service = CreateService();
        var created = await service.CreateAsync(new CreateEventDto
        {
            Title = "Удаляемое событие",
            TotalSeats = 10,
            StartAt = Utc(2026, 4, 5, 10, 0, 0),
            EndAt = Utc(2026, 4, 5, 11, 0, 0)
        });

        // Act
        await service.DeleteAsync(created.Id);
        var result = await service.GetAllAsync(new EventQueryDto());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Проверяет фильтрацию событий по диапазону дат.
    /// </summary>
    [Fact]
    public async Task GetAll_ShouldFilterByDates()
    {
        // Arrange
        var service = CreateService();

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Раннее событие",
            TotalSeats = 10,
            StartAt = Utc(2026, 4, 1, 10, 0, 0),
            EndAt = Utc(2026, 4, 1, 11, 0, 0)
        });

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Подходящее событие",
            TotalSeats = 10,
            StartAt = Utc(2026, 4, 10, 10, 0, 0),
            EndAt = Utc(2026, 4, 10, 11, 0, 0)
        });

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Позднее событие",
            TotalSeats = 10,
            StartAt = Utc(2026, 4, 20, 10, 0, 0),
            EndAt = Utc(2026, 4, 20, 11, 0, 0)
        });

        // Act
        var result = await service.GetAllAsync(new EventQueryDto
        {
            From = Utc(2026, 4, 5, 0, 0, 0),
            To = Utc(2026, 4, 15, 23, 59, 59)
        });

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Single().Title.Should().Be("Подходящее событие");
    }

    /// <summary>
    /// Проверяет пагинацию событий.
    /// </summary>
    [Fact]
    public async Task GetAll_ShouldApplyPagination()
    {
        // Arrange
        var service = CreateService();

        for (var i = 1; i <= 5; i++)
        {
            await service.CreateAsync(new CreateEventDto
            {
                Title = $"Событие {i}",
                TotalSeats = 10,
                StartAt = Utc(2026, 4, i, 10, 0, 0),
                EndAt = Utc(2026, 4, i, 11, 0, 0)
            });
        }

        // Act
        var result = await service.GetAllAsync(new EventQueryDto
        {
            Page = 2,
            PageSize = 2
        });

        // Assert
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.CurrentCount.Should().Be(2);

        result.Items.Select(x => x.Title)
            .Should()
            .ContainInOrder("Событие 3", "Событие 4");
    }

    /// <summary>
    /// Проверяет, что попытка получить событие по несуществующему идентификатору приводит к исключению.
    /// </summary>
    [Fact]
    public async Task GetById_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var service = CreateService();
        var id = Guid.NewGuid();

        // Act
        // Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(id));
    }

    /// <summary>
    /// Проверяет, что попытка обновить несуществующее событие приводит к исключению.
    /// </summary>
    [Fact]
    public async Task Update_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var service = CreateService();
        var id = Guid.NewGuid();

        var dto = new UpdateEventDto
        {
            Title = "Обновление",
            StartAt = Utc(2026, 4, 15, 10, 0, 0),
            EndAt = Utc(2026, 4, 15, 11, 0, 0)
        };

        // Act
        // Assert
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(id, dto));
    }

    /// <summary>
    /// Проверяет, что отмена токена приводит к прерыванию операции.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldThrowOperationCanceled_WhenTokenIsCancelled()
    {
        // Arrange
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var dto = new CreateEventDto
        {
            Title = "Отменяемое событие",
            TotalSeats = 10,
            StartAt = Utc(2026, 4, 16, 10, 0, 0),
            EndAt = Utc(2026, 4, 16, 11, 0, 0)
        };

        // Act + Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.CreateAsync(dto, cts.Token));
    }

    /// <summary>
    /// Проверяет валидацию DTO создания события с некорректными данными.
    /// </summary>
    [Fact]
    public void CreateEventDto_ShouldBeInvalid_WhenRequiredFieldsAreMissing()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = string.Empty,
            StartAt = null,
            EndAt = null
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(x => x.ErrorMessage!.Contains("Заголовок"));
        results.Should().Contain(x => x.ErrorMessage!.Contains("Дата начала"));
        results.Should().Contain(x => x.ErrorMessage!.Contains("Дата окончания"));
        results.Should().Contain(x => x.ErrorMessage!.Contains("Количество мест"));
    }

    /// <summary>
    /// Проверяет валидацию DTO обновления события, если дата окончания раньше даты начала.
    /// </summary>
    [Fact]
    public void UpdateEventDto_ShouldBeInvalid_WhenEndAtEarlierThanStartAt()
    {
        // Arrange
        var dto = new UpdateEventDto
        {
            Title = "Некорректное событие",
            StartAt = Utc(2026, 4, 20, 12, 0, 0),
            EndAt = Utc(2026, 4, 20, 11, 0, 0)
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(x =>
            x.MemberNames.Contains(nameof(UpdateEventDto.EndAt)) &&
            x.ErrorMessage == "Дата окончания должна быть больше даты начала");
    }

    /// <summary>
    /// Проверяет валидацию query DTO, если дата начала позже даты окончания.
    /// </summary>
    [Fact]
    public void EventQueryDto_ShouldBeInvalid_WhenFromLaterThanTo()
    {
        // Arrange
        var dto = new EventQueryDto
        {
            From = Utc(2026, 5, 1, 0, 0, 0),
            To = Utc(2026, 1, 1, 0, 0, 0)
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().ContainSingle(x =>
            x.MemberNames.Contains(nameof(EventQueryDto.From)) &&
            x.MemberNames.Contains(nameof(EventQueryDto.To)) &&
            x.ErrorMessage == "Дата начала не может быть позже даты окончания");
    }

    /// <summary>
    /// Создает экземпляр сервиса для тестов.
    /// </summary>
    private IEventService CreateService()
    {
        return GetRequiredService<IEventService>();
    }

    /// <summary>
    /// Выполняет валидацию модели через DataAnnotations.
    /// </summary>
    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);

        Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        return results;
    }

}
