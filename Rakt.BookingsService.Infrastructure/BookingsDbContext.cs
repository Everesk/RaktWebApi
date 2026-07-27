using Microsoft.EntityFrameworkCore;
using Rakt.BookingsService.Domain;
namespace Rakt.BookingsService.Infrastructure;
/// <summary>Контекст изолированной базы данных броней.</summary>
public sealed class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    /// <summary>Брони сервиса.</summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>
    /// Настраивает схему изолированной базы данных броней.
    /// </summary>
    /// <param name="builder">Построитель модели EF Core.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");
            entity.HasKey(booking => booking.Id);
            entity.Property(booking => booking.Status).HasConversion<string>();
            entity.Property(booking => booking.CreatedAt).IsRequired();
            entity.Property(booking => booking.ProcessedAt);
            entity.HasIndex(booking => new { booking.UserId, booking.EventId });
        });
    }
}
