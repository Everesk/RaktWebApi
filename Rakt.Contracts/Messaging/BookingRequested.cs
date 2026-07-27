namespace Rakt.Contracts.Messaging;
/// <summary>Сообщение о запросе места для брони.</summary>
public sealed record BookingRequested(Guid BookingId, Guid UserId, Guid EventId, DateTimeOffset OccurredAt);
