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
}
