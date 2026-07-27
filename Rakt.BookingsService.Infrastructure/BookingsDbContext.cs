using Microsoft.EntityFrameworkCore;
using Rakt.BookingsService.Domain;
namespace Rakt.BookingsService.Infrastructure;
/// <summary>Контекст изолированной базы данных броней.</summary>
public sealed class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    /// <summary>Брони сервиса.</summary>
    public DbSet<Booking> Bookings => Set<Booking>();
    protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<Booking>(e => { e.ToTable("bookings"); e.HasKey(x => x.Id); e.Property(x => x.Status).HasConversion<string>(); e.HasIndex(x => new { x.UserId, x.EventId }); });
}
