using Rakt.BookingsService.Domain;
using Rakt.Contracts.Messaging;
namespace Rakt.BookingsService.Application;
/// <summary>Сценарии создания и отмены броней.</summary>
public sealed class BookingService(IBookingRepository bookings, IBookingMessagePublisher publisher)
{
    /// <summary>Создаёт ожидающую бронь и публикует запрос места.</summary>
    public async Task<Booking> CreateAsync(Guid userId, Guid eventId, CancellationToken ct = default)
    {
        var booking = Booking.Create(userId, eventId);

        await bookings.AddAsync(booking, ct);
        await bookings.SaveChangesAsync(ct);
        await publisher.PublishAsync(
            new BookingRequested(booking.Id, userId, eventId, DateTimeOffset.UtcNow),
            ct);

        return booking;
    }
    /// <summary>Отменяет бронь и публикует запрос возврата места.</summary>
    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var booking = await bookings.GetAsync(id, ct)
            ?? throw new KeyNotFoundException("Бронь не найдена.");

        booking.Cancel();

        await bookings.SaveChangesAsync(ct);
        await publisher.PublishAsync(
            new BookingCancelled(booking.Id, booking.EventId, DateTimeOffset.UtcNow),
            ct);
    }
}
