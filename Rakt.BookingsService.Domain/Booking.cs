namespace Rakt.BookingsService.Domain;

/// <summary>Состояние обработки бронирования.</summary>
public enum BookingStatus { Pending, Confirmed, Rejected, Cancelled }

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
    private Booking() { }
    private Booking(Guid userId, Guid eventId) => (UserId, EventId) = (userId, eventId);
    /// <summary>Создаёт ожидающую подтверждения бронь.</summary>
    public static Booking Create(Guid userId, Guid eventId) => new(userId, eventId);
    /// <summary>Подтверждает бронь после ответа сервиса событий.</summary>
    public void Confirm() => Status = BookingStatus.Confirmed;
    /// <summary>Отклоняет бронь после отказа сервиса событий.</summary>
    public void Reject() => Status = BookingStatus.Rejected;
    /// <summary>Отменяет активную бронь.</summary>
    public void Cancel() { if (Status == BookingStatus.Cancelled) throw new InvalidOperationException("Бронь уже отменена."); Status = BookingStatus.Cancelled; }
}
