using Rakt.BookingsService.Application;
using Rakt.BookingsService.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Проверки прикладного сценария фоновой обработки броней.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BookingProcessingServiceTests
{
    /// <summary>
    /// Обработчик должен подтвердить только брони в статусе ожидания.
    /// </summary>
    [Fact]
    public async Task ProcessPendingAsync_ConfirmsOnlyPendingBookings()
    {
        var repository = new InMemoryBookingRepository();
        var publisher = new RecordingBookingConfirmedPublisher(
            () => repository.SaveChangesCount);
        var confirmationService = new BookingConfirmationService(repository, publisher);
        var processingState = new BookingProcessingState();
        var processingService = new BookingProcessingService(
            repository,
            confirmationService,
            processingState,
            NullLogger<BookingProcessingService>.Instance);
        var pendingBooking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        var cancelledBooking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        cancelledBooking.Cancel();
        await repository.AddAsync(pendingBooking);
        await repository.AddAsync(cancelledBooking);

        await processingService.ProcessPendingAsync(attemptsLimit: 3);

        Assert.Equal(BookingStatus.Confirmed, pendingBooking.Status);
        Assert.Equal(BookingStatus.Cancelled, cancelledBooking.Status);
        Assert.NotNull(publisher.PublishedMessage);
        Assert.Equal(pendingBooking.Id, publisher.PublishedMessage.BookingId);
    }

    /// <summary>
    /// После лимита ошибок публикации обработчик должен отклонить бронь.
    /// </summary>
    [Fact]
    public async Task ProcessPendingAsync_RejectsBookingAfterAttemptsLimit()
    {
        var repository = new InMemoryBookingRepository();
        var publisher = new FailingBookingConfirmedPublisher();
        var confirmationService = new BookingConfirmationService(repository, publisher);
        var processingState = new BookingProcessingState();
        var processingService = new BookingProcessingService(
            repository,
            confirmationService,
            processingState,
            NullLogger<BookingProcessingService>.Instance);
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(booking);

        await processingService.ProcessPendingAsync(attemptsLimit: 2);

        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.ProcessedAt);

        await processingService.ProcessPendingAsync(attemptsLimit: 2);

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.Equal(2, publisher.PublishAttemptsCount);
    }
}
