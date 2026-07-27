namespace Rakt.BookingsService.Domain;

using Rakt.BookingsService.Domain.Exceptions;

/// <summary>Бронь, хранящая только идентификаторы внешних агрегатов.</summary>
public sealed class Booking
{
    /// <summary>Идентификатор брони.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();
    /// <summary>Идентификатор пользователя без внешнего ключа.</summary>
    public Guid UserId { get; private set; }
    /// <summary>Идентификатор события без внешнего ключа.</summary>
    public Guid EventId { get; private set; }
    /// <summary>Статус брони.</summary>
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    /// <summary>Момент создания брони.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Момент подтверждения или отклонения брони.</summary>
    public DateTimeOffset? ProcessedAt { get; private set; }
    private Booking()
    {
    }

    private Booking(Guid userId, Guid eventId)
    {
        UserId = userId;
        EventId = eventId;
    }
    /// <summary>Создаёт ожидающую подтверждения бронь.</summary>
    public static Booking Create(Guid userId, Guid eventId)
    {
        return new Booking(userId, eventId);
    }
    /// <summary>Подтверждает бронь после фоновой обработки.</summary>
    /// <param name="processedAt">Момент подтверждения.</param>
    public void Confirm(DateTimeOffset processedAt)
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = processedAt;
    }

    /// <summary>Отклоняет бронь после исчерпания попыток обработки.</summary>
    /// <param name="processedAt">Момент отклонения.</param>
    public void Reject(DateTimeOffset processedAt)
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = processedAt;
    }

    /// <summary>Возвращает бронь в ожидание после временной ошибки публикации.</summary>
    public void ReturnToPending()
    {
        Status = BookingStatus.Pending;
        ProcessedAt = null;
    }
    /// <summary>Отменяет активную бронь.</summary>
    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new BookingAlreadyCancelledException("Бронирование уже отменено.");
        }

        Status = BookingStatus.Cancelled;
    }
}
