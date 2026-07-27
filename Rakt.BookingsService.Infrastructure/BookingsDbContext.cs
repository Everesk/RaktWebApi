using Microsoft.EntityFrameworkCore;
using Rakt.BookingsService.Domain;
namespace Rakt.BookingsService.Infrastructure;
/// <summary>Контекст изолированной базы данных броней.</summary>
public sealed class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    /// <summary>Брони сервиса.</summary>
    public DbSet<Booking> Bookings => Set<Booking>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");
            entity.HasKey(booking => booking.Id);
            entity.Property(booking => booking.Status).HasConversion<string>();
            entity.HasIndex(booking => new { booking.UserId, booking.EventId });
        });
    }
}
