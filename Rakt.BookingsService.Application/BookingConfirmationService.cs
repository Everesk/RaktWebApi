using Rakt.BookingsService.Domain.Exceptions;
using Rakt.Contracts.Messaging;

namespace Rakt.BookingsService.Application;

/// <summary>Подтверждает бронь и публикует соответствующее интеграционное событие.</summary>
public sealed class BookingConfirmationService(
    IBookingRepository bookings,
    IBookingConfirmedPublisher publisher)
{
    /// <summary>Сохраняет подтверждённый статус брони и затем уведомляет Kafka.</summary>
    public async Task ConfirmAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await bookings.GetAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException($"Бронь с идентификатором '{bookingId}' не найдена.");

        booking.Confirm(DateTimeOffset.UtcNow);

        await bookings.SaveChangesAsync(cancellationToken);

        var message = new BookingConfirmed(
            booking.Id,
            booking.EventId,
            booking.UserId,
            SeatsCount: 1,
            ConfirmedAt: DateTimeOffset.UtcNow);

        await publisher.PublishAsync(message, cancellationToken);
    }
}
