namespace Rakt.Contracts.Messaging;
/// <summary>Сообщение об успешном резервировании мест.</summary>
public sealed record SeatsReserved(Guid BookingId, Guid EventId, DateTimeOffset OccurredAt);
