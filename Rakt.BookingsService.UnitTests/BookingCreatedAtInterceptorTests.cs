using Microsoft.EntityFrameworkCore;
using Rakt.BookingsService.Domain;
using Rakt.BookingsService.Infrastructure;
using Xunit;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Проверки автоматического заполнения времени создания брони.
/// </summary>
public sealed class BookingCreatedAtInterceptorTests
{
    /// <summary>
    /// Перехватчик EF Core должен установить время создания новой брони.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_SetsCreatedAtForNewBooking()
    {
        var interceptor = new BookingCreatedAtInterceptor();
        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        await using var context = new BookingsDbContext(options);
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        context.Bookings.Add(booking);

        await context.SaveChangesAsync();

        Assert.NotEqual(default, booking.CreatedAt);
    }
}
