using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using RaktWebApi.Common.Exceptions;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;
using RaktWebApi.Models.DTO;
using RaktWebApi.Services;

namespace Rakt.Tests.Services;

/// <summary>
/// Набор тестов для сервиса <see cref="EventService"/>.
/// </summary>
public class EventServiceTests
{
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
            Description = "Обсуждение задач",
            StartAt = new DateTime(2026, 4, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 1, 11, 0, 0)
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
            StartAt = new DateTime(2026, 4, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 1, 11, 0, 0)
        });

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Событие 2",
            StartAt = new DateTime(2026, 4, 2, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 2, 11, 0, 0)
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
            StartAt = new DateTime(2026, 4, 3, 9, 0, 0),
            EndAt = new DateTime(2026, 4, 3, 10, 0, 0)
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
            Description = "Старое описание",
            StartAt = new DateTime(2026, 4, 4, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 4, 11, 0, 0)
        });

        var dto = new UpdateEventDto
        {
            Title = "Новый заголовок",
            Description = "Новое описание",
            StartAt = new DateTime(2026, 4, 4, 12, 0, 0),
            EndAt = new DateTime(2026, 4, 4, 13, 0, 0)
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
            StartAt = new DateTime(2026, 4, 5, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 5, 11, 0, 0)
        });

        // Act
        await service.DeleteAsync(created.Id);
        var result = await service.GetAllAsync(new EventQueryDto());

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Проверяет фильтрацию событий по названию.
    /// </summary>
    [Fact]
    public async Task GetAll_ShouldFilterByTitle()
    {
        // Arrange
        var service = CreateService();

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Встреча команды",
            StartAt = new DateTime(2026, 4, 6, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 6, 11, 0, 0)
        });

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Созвон с заказчиком",
            StartAt = new DateTime(2026, 4, 6, 12, 0, 0),
            EndAt = new DateTime(2026, 4, 6, 13, 0, 0)
        });

        // Act
        var result = await service.GetAllAsync(new EventQueryDto
        {
            Title = "встреча"
        });

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items.Single().Title.Should().Be("Встреча команды");
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
            StartAt = new DateTime(2026, 4, 1, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 1, 11, 0, 0)
        });

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Подходящее событие",
            StartAt = new DateTime(2026, 4, 10, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 10, 11, 0, 0)
        });

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Позднее событие",
            StartAt = new DateTime(2026, 4, 20, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 20, 11, 0, 0)
        });

        // Act
        var result = await service.GetAllAsync(new EventQueryDto
        {
            From = new DateTime(2026, 4, 5, 0, 0, 0),
            To = new DateTime(2026, 4, 15, 23, 59, 59)
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
                StartAt = new DateTime(2026, 4, i, 10, 0, 0),
                EndAt = new DateTime(2026, 4, i, 11, 0, 0)
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
    /// Проверяет совместную работу фильтрации и пагинации.
    /// </summary>
    [Fact]
    public async Task GetAll_ShouldApplyCombinedFiltering()
    {
        // Arrange
        var service = CreateService();

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Встреча backend",
            StartAt = new DateTime(2026, 4, 10, 9, 0, 0),
            EndAt = new DateTime(2026, 4, 10, 10, 0, 0)
        });

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Встреча frontend",
            StartAt = new DateTime(2026, 4, 11, 9, 0, 0),
            EndAt = new DateTime(2026, 4, 11, 10, 0, 0)
        });

        await service.CreateAsync(new CreateEventDto
        {
            Title = "Созвон backend",
            StartAt = new DateTime(2026, 4, 12, 9, 0, 0),
            EndAt = new DateTime(2026, 4, 12, 10, 0, 0)
        });

        // Act
        var result = await service.GetAllAsync(new EventQueryDto
        {
            Title = "встреча",
            From = new DateTime(2026, 4, 10, 0, 0, 0),
            To = new DateTime(2026, 4, 11, 23, 59, 59),
            Page = 1,
            PageSize = 1
        });

        // Assert
        result.TotalCount.Should().Be(2);
        result.CurrentCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Single().Title.Should().Be("Встреча backend");
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
            StartAt = new DateTime(2026, 4, 15, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 15, 11, 0, 0)
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
            StartAt = new DateTime(2026, 4, 16, 10, 0, 0),
            EndAt = new DateTime(2026, 4, 16, 11, 0, 0)
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
            StartAt = new DateTime(2026, 4, 20, 12, 0, 0),
            EndAt = new DateTime(2026, 4, 20, 11, 0, 0)
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
            From = new DateTime(2026, 5, 1, 0, 0, 0),
            To = new DateTime(2026, 1, 1, 0, 0, 0)
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
    private static EventService CreateService()
    {
        var repository = new InMemoryEventRepository();
        return new EventService(repository);
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
