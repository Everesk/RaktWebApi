namespace RaktWebApi.Models;

/// <summary>
/// Представляет событие (например, встречу или задачу) с временными рамками.
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
    public DateTime StartAt { get; private set; }

    /// <summary>
    /// Дата и время окончания события.
    /// </summary>
    public DateTime EndAt { get; private set; }

    public Event(string title, string? description, DateTime startAt, DateTime endAt)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }

    public void Update(string title, string? description, DateTime startAt, DateTime endAt)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }
}