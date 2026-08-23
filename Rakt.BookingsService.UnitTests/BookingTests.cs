using Rakt.BookingsService.Domain;
using Rakt.BookingsService.Domain.Exceptions;
using Xunit;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Проверки доменной модели брони.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BookingTests
{
    /// <summary>
    /// Новая бронь должна содержать переданные идентификаторы и ожидать подтверждения.
    /// </summary>
    [Fact]
    public void Create_InitializesPendingBooking()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var booking = Booking.Create(userId, eventId);

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(userId, booking.UserId);
        Assert.Equal(eventId, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    /// <summary>
    /// Отмена активной брони должна изменить её статус.
    /// </summary>
    [Fact]
    public void Cancel_ChangesStatusToCancelled()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    /// <summary>
    /// Повторная отмена брони запрещена.
    /// </summary>
    [Fact]
    public void Cancel_ThrowsWhenAlreadyCancelled()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        booking.Cancel();

        Assert.Throws<BookingAlreadyCancelledException>(booking.Cancel);
    }

    /// <summary>
    /// Подтверждение брони должно изменить её статус.
    /// </summary>
    [Fact]
    public void Confirm_ChangesStatusToConfirmed()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

        booking.Confirm(DateTimeOffset.UtcNow);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    /// <summary>
    /// Отклонение брони должно изменить её статус.
    /// </summary>
    [Fact]
    public void Reject_ChangesStatusToRejected()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

        booking.Reject(DateTimeOffset.UtcNow);

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
}
