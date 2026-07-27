namespace Rakt.Contracts.Messaging;
/// <summary>Публичное событие о подтверждении брони.</summary>
public sealed record BookingConfirmed(Guid BookingId, Guid EventId, Guid UserId, int SeatsCount, DateTimeOffset ConfirmedAt);
