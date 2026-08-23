namespace Rakt.EventsService.Domain;

/// <summary>
/// Состояние обработки брони сервисом событий для идемпотентного резервирования и отмены.
/// </summary>
public sealed class BookingSeatReservation
{
    /// <summary>
    /// Идентификатор брони из сервиса броней.
    /// </summary>
    public Guid BookingId { get; private set; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Признак фактически занятого места.
    /// </summary>
    public bool IsSeatReserved { get; private set; }

    /// <summary>
    /// Признак отмены брони, в том числе до получения запроса на резервирование.
    /// </summary>
    public bool IsCancelled { get; private set; }

    private BookingSeatReservation()
    {
    }

    private BookingSeatReservation(Guid bookingId, Guid eventId, bool isSeatReserved, bool isCancelled)
    {
        BookingId = bookingId;
        EventId = eventId;
        IsSeatReserved = isSeatReserved;
        IsCancelled = isCancelled;
    }

    /// <summary>
    /// Создаёт запись об успешном резервировании места.
    /// </summary>
    public static BookingSeatReservation CreateReserved(Guid bookingId, Guid eventId)
    {
        return new BookingSeatReservation(bookingId, eventId, isSeatReserved: true, isCancelled: false);
    }

    /// <summary>
    /// Создаёт запись об отмене, пришедшей раньше запроса на резервирование.
    /// </summary>
    public static BookingSeatReservation CreateCancelled(Guid bookingId, Guid eventId)
    {
        return new BookingSeatReservation(bookingId, eventId, isSeatReserved: false, isCancelled: true);
    }

    /// <summary>
    /// Отмечает отмену и сообщает, требуется ли вернуть ранее занятое место.
    /// </summary>
    public bool Cancel()
    {
        if (IsCancelled)
        {
            return false;
        }

        IsCancelled = true;
        var shouldReleaseSeat = IsSeatReserved;
        IsSeatReserved = false;

        return shouldReleaseSeat;
    }
}
