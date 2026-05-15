using System.ComponentModel.DataAnnotations;

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
    /// Общее количество мест на событии.
    /// </summary>
    public int TotalSeats { get; private set; }

    /// <summary>
    /// Текущее количество свободных мест.
    /// </summary>
    public int AvailableSeats { get; private set; }

    /// <summary>
    /// Признак того, что свободных мест на событии больше нет.
    /// </summary>
    public bool IsFull => AvailableSeats == 0;

    /// <summary>
    /// Основной конструктор.
    /// </summary>
    internal Event(string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt, int totalSeats)
    {
        ValidateTotalSeats(totalSeats);

        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    internal static Event Create(string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt, int totalSeats)
    {
        return new Event(title, description, startAt, endAt, totalSeats);
    }

    /// <summary>
    /// Полностью обновляет все свойства.
    /// </summary>
    internal void Update(string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }

    /// <summary>
    /// Пытается зарезервировать указанное количество мест.
    /// </summary>
    public bool TryReserveSeats(int count = 1)
    {
        if (count <= 0 || AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;
        return true;
    }

    /// <summary>
    /// Освобождает указанное количество мест.
    /// </summary>
    public void ReleaseSeats(int count = 1)
    {
        if (count <= 0)
        {
            return;
        }

        AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
    }

    private static void ValidateTotalSeats(int totalSeats)
    {
        if (totalSeats <= 0)
        {
            throw new ValidationException("Количество мест на событии должно быть больше нуля.");
        }
    }
}
