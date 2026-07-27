namespace Rakt.EventsService.Domain;

/// <summary>Событие и его остаток мест, принадлежащие сервису событий.</summary>
public sealed class Event
{
    /// <summary>Идентификатор события.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();
    /// <summary>Название события.</summary>
    public string Title { get; private set; }
    /// <summary>Время начала.</summary>
    public DateTimeOffset StartAt { get; private set; }
    /// <summary>Общее число мест.</summary>
    public int TotalSeats { get; private set; }
    /// <summary>Оставшееся число мест.</summary>
    public int AvailableSeats { get; private set; }

    private Event() { Title = null!; }
    private Event(string title, DateTimeOffset startAt, int totalSeats)
    {
        if (totalSeats <= 0) throw new ArgumentOutOfRangeException(nameof(totalSeats));
        Title = title; StartAt = startAt; TotalSeats = AvailableSeats = totalSeats;
    }
    /// <summary>Создаёт новое событие.</summary>
    public static Event Create(string title, DateTimeOffset startAt, int totalSeats) => new(title, startAt, totalSeats);
    /// <summary>Пытается занять одно место.</summary>
    public bool TryReserveSeat() { if (AvailableSeats == 0) return false; AvailableSeats--; return true; }
    /// <summary>Возвращает одно место без превышения вместимости.</summary>
    public void ReleaseSeat() => AvailableSeats = Math.Min(TotalSeats, AvailableSeats + 1);
}
