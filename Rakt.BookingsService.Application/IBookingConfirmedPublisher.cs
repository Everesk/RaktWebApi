using Rakt.Contracts.Messaging;

namespace Rakt.BookingsService.Application;

/// <summary>Публикует интеграционное событие о подтверждённой брони.</summary>
public interface IBookingConfirmedPublisher
{
    /// <summary>Отправляет подтверждённую бронь подписчикам.</summary>
    Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default);
}
