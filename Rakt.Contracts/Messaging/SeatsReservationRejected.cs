namespace Rakt.Contracts.Messaging;
/// <summary>Сообщение об отказе в резервировании мест.</summary>
public sealed record SeatsReservationRejected(Guid BookingId, Guid EventId, string Reason, DateTimeOffset OccurredAt);
