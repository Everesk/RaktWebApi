using Rakt.Contracts.Messaging;
namespace Rakt.BookingsService.Application;
/// <summary>Порт публикации интеграционных событий.</summary>
public interface IBookingMessagePublisher { Task PublishAsync(BookingRequested message, CancellationToken ct = default); Task PublishAsync(BookingCancelled message, CancellationToken ct = default); }
