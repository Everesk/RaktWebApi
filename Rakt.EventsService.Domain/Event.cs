namespace Rakt.EventsService.Domain;

using Rakt.EventsService.Domain.Exceptions;

/// <summary>Событие и его остаток мест, принадлежащие сервису событий.</summary>
public sealed class Event
{
    /// <summary>Идентификатор события.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();
    /// <summary>Название события.</summary>
    public string Title { get; private set; }

    /// <summary>Описание события.</summary>
    public string? Description { get; private set; }
    /// <summary>Время начала.</summary>
    public DateTimeOffset StartAt { get; private set; }

    /// <summary>Время окончания.</summary>
    public DateTimeOffset EndAt { get; private set; }
    /// <summary>Общее число мест.</summary>
    public int TotalSeats { get; private set; }
    /// <summary>Оставшееся число мест.</summary>
    public int AvailableSeats { get; private set; }

    /// <summary>Признак отсутствия свободных мест.</summary>
    public bool IsFull => AvailableSeats == 0;

    private Event()
    {
        Title = null!;
    }
    private Event(
        string title,
        string? description,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        int totalSeats)
    {
        if (totalSeats <= 0)
        {
            throw new InvalidTotalSeatsException(
                "Количество мест на событии должно быть больше нуля.");
        }
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }
    /// <summary>Создаёт новое событие.</summary>
    public static Event Create(
        string title,
        string? description,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        int totalSeats)
    {
        return new Event(title, description, startAt, endAt, totalSeats);
    }

    /// <summary>Обновляет изменяемые свойства события.</summary>
    public void Update(string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }
    /// <summary>Пытается занять одно место.</summary>
    public bool TryReserveSeat()
    {
        if (AvailableSeats == 0)
        {
            return false;
        }

        AvailableSeats--;
        return true;
    }
    /// <summary>Возвращает одно место без превышения вместимости.</summary>
    public void ReleaseSeat() => AvailableSeats = Math.Min(TotalSeats, AvailableSeats + 1);

    /// <summary>Пытается зарезервировать указанное количество мест.</summary>
    public bool TryReserveSeats(int count = 1)
    {
        if (count <= 0 || AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;
        return true;
    }

    /// <summary>Возвращает указанное количество мест.</summary>
    public void ReleaseSeats(int count = 1)
    {
        if (count <= 0)
        {
            return;
        }

        AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
    }
}
