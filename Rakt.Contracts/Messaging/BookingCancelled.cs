namespace Rakt.Contracts.Messaging;
/// <summary>Сообщение об отмене брони и необходимости вернуть место.</summary>
public sealed record BookingCancelled(Guid BookingId, Guid EventId, DateTimeOffset OccurredAt);
