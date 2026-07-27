using Rakt.BookingsService.Application;
using Rakt.BookingsService.Domain;
using Rakt.BookingsService.Domain.Exceptions;
using Xunit;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Проверки локальных сценариев создания и отмены броней.
/// </summary>
public sealed class BookingServiceTests
{
    /// <summary>
    /// Создание брони сохраняет её и публикует запрос на резервирование места.
    /// </summary>
    [Fact]
    public async Task CreateAsync_SavesPendingBookingAndPublishesRequest()
    {
        var repository = new InMemoryBookingRepository();
        var publisher = new RecordingBookingMessagePublisher();
        var service = new BookingService(repository, publisher);
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var booking = await service.CreateAsync(userId, eventId);

        var storedBooking = await repository.GetAsync(booking.Id);
        Assert.Same(booking, storedBooking);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(1, repository.SaveChangesCount);
        Assert.NotNull(publisher.RequestedMessage);
        Assert.Equal(booking.Id, publisher.RequestedMessage.BookingId);
        Assert.Equal(userId, publisher.RequestedMessage.UserId);
        Assert.Equal(eventId, publisher.RequestedMessage.EventId);
    }

    /// <summary>
    /// Отмена существующей брони сохраняет статус и публикует запрос возврата места.
    /// </summary>
    [Fact]
    public async Task CancelAsync_CancelsBookingAndPublishesCancellation()
    {
        var repository = new InMemoryBookingRepository();
        var publisher = new RecordingBookingMessagePublisher();
        var service = new BookingService(repository, publisher);
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(booking);

        await service.CancelAsync(booking.Id);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(1, repository.SaveChangesCount);
        Assert.NotNull(publisher.CancelledMessage);
        Assert.Equal(booking.Id, publisher.CancelledMessage.BookingId);
        Assert.Equal(booking.EventId, publisher.CancelledMessage.EventId);
    }

    /// <summary>
    /// Отмена отсутствующей брони должна сообщать о её отсутствии.
    /// </summary>
    [Fact]
    public async Task CancelAsync_ThrowsWhenBookingDoesNotExist()
    {
        var repository = new InMemoryBookingRepository();
        var publisher = new RecordingBookingMessagePublisher();
        var service = new BookingService(repository, publisher);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.CancelAsync(Guid.NewGuid()));

        Assert.StartsWith("Бронь с идентификатором", exception.Message);
        Assert.Null(publisher.CancelledMessage);
    }
}
