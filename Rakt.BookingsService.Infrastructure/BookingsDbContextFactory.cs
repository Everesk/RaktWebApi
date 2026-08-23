using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Rakt.BookingsService.Infrastructure;
/// <summary>Создаёт контекст броней для инструментов EF Core.</summary>
public sealed class BookingsDbContextFactory : IDesignTimeDbContextFactory<BookingsDbContext>
{
    public BookingsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseNpgsql("Host=localhost;Database=rakt_bookings;Username=postgres;Password=postgres")
            .Options;

        return new BookingsDbContext(options);
    }
}
