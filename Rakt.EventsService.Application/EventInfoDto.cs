using Rakt.EventsService.Domain;

namespace Rakt.EventsService.Application;
/// <summary>Данные события для API.</summary>
public sealed class EventInfoDto
{
    /// <summary>Идентификатор.</summary>
    public Guid Id { get; init; }
    /// <summary>Заголовок.</summary>
    public string Title { get; init; } = string.Empty;
    /// <summary>Описание.</summary>
    public string? Description { get; init; }
    /// <summary>Время начала.</summary>
    public DateTimeOffset StartAt { get; init; }
    /// <summary>Время окончания.</summary>
    public DateTimeOffset EndAt { get; init; }
    /// <summary>Общее количество мест.</summary>
    public int TotalSeats { get; init; }
    /// <summary>Свободные места.</summary>
    public int AvailableSeats { get; init; }
    /// <summary>Признак заполненности.</summary>
    public bool IsFull { get; init; }

    /// <summary>
    /// Создаёт DTO для передачи данных указанного события.
    /// </summary>
    /// <param name="entity">Событие из доменной модели.</param>
    /// <returns>Данные события для API и кеша.</returns>
    public static EventInfoDto FromEntity(Event entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        StartAt = entity.StartAt,
        EndAt = entity.EndAt,
        TotalSeats = entity.TotalSeats,
        AvailableSeats = entity.AvailableSeats,
        IsFull = entity.IsFull
    };
}
