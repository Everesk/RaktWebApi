using Rakt.BookingsService.Application;
using Rakt.BookingsService.Domain;
using Rakt.BookingsService.Domain.Exceptions;
using Xunit;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Проверки подтверждения брони после ответа сервиса событий.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BookingConfirmationServiceTests
{
    /// <summary>
    /// Подтверждение должно сохранить статус до публикации интеграционного события.
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_SavesConfirmedBookingBeforePublishingEvent()
    {
        var repository = new InMemoryBookingRepository();
        var publisher = new RecordingBookingConfirmedPublisher(
            () => repository.SaveChangesCount);
        var service = new BookingConfirmationService(repository, publisher);
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(booking);

        await service.ConfirmAsync(booking.Id);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(1, repository.SaveChangesCount);
        Assert.Equal(1, publisher.SaveChangesCountAtPublication);
        Assert.NotNull(publisher.PublishedMessage);
        Assert.Equal(booking.Id, publisher.PublishedMessage.BookingId);
        Assert.Equal(booking.EventId, publisher.PublishedMessage.EventId);
        Assert.Equal(booking.UserId, publisher.PublishedMessage.UserId);
        Assert.Equal(1, publisher.PublishedMessage.SeatsCount);
    }

    /// <summary>
    /// Подтверждение отсутствующей брони не должно публиковать событие.
    /// </summary>
    [Fact]
    public async Task ConfirmAsync_ThrowsWhenBookingDoesNotExist()
    {
        var repository = new InMemoryBookingRepository();
        var publisher = new RecordingBookingConfirmedPublisher(
            () => repository.SaveChangesCount);
        var service = new BookingConfirmationService(repository, publisher);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.ConfirmAsync(Guid.NewGuid()));

        Assert.Equal(0, repository.SaveChangesCount);
        Assert.Null(publisher.PublishedMessage);
    }
}
