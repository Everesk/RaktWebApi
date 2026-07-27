using Rakt.BookingsService.Domain;
using Xunit;
namespace Rakt.BookingsService.UnitTests;
/// <summary>Проверки доменной модели броней.</summary>
public sealed class BookingTests
{
    /// <summary>Повторная отмена брони запрещена.</summary>
    [Fact]
    public void Cancel_ThrowsWhenAlreadyCancelled()
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        booking.Cancel();
        Assert.Throws<InvalidOperationException>(booking.Cancel);
    }
}
