namespace Rakt.Contracts.Messaging;

/// <summary>Имена Kafka-топиков, используемые контрактами бронирований.</summary>
public static class BookingTopics
{
    /// <summary>Топик с подтверждёнными бронями.</summary>
    public const string Confirmed = "booking-confirmed";
}

/// <summary>
/// Публичное событие, уведомляющее о подтверждении брони.
/// Содержит только данные, необходимые подписчикам.
/// </summary>
/// <param name="BookingId">Идентификатор подтверждённой брони.</param>
/// <param name="EventId">Идентификатор события.</param>
/// <param name="UserId">Идентификатор пользователя.</param>
/// <param name="SeatsCount">Количество подтверждённых мест.</param>
/// <param name="ConfirmedAt">Момент подтверждения брони.</param>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTimeOffset ConfirmedAt);

/// <summary>Сообщение о запросе места для брони.</summary>
public sealed record BookingRequested(Guid BookingId, Guid UserId, Guid EventId, DateTimeOffset OccurredAt);

/// <summary>Сообщение об успешном резервировании мест.</summary>
public sealed record SeatsReserved(Guid BookingId, Guid EventId, DateTimeOffset OccurredAt);

/// <summary>Сообщение об отказе в резервировании мест.</summary>
public sealed record SeatsReservationRejected(Guid BookingId, Guid EventId, string Reason, DateTimeOffset OccurredAt);

/// <summary>Сообщение об отмене брони и необходимости вернуть место.</summary>
public sealed record BookingCancelled(Guid BookingId, Guid EventId, DateTimeOffset OccurredAt);
