namespace RaktWebApi.Models;

/// <summary>
/// Представляет событие.
/// </summary>
public class Event
{
    /// <summary>
    /// Уникальный идентификатор события.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Заголовок события.
    /// </summary>
    public string Title { get; private set; } 

    /// <summary>
    /// Описание события.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Дата и время начала события.
    /// </summary>
    public DateTimeOffset StartAt { get; private set; }

    /// <summary>
    /// Дата и время окончания события.
    /// </summary>
    public DateTimeOffset EndAt { get; private set; }

    /// <summary>
    /// Основной конструктор
    /// </summary>
    
    internal Event(string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }

    /// <summary>
    /// Полностью обновляет все свойства
    /// </summary>
    internal void Update(string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }
}
