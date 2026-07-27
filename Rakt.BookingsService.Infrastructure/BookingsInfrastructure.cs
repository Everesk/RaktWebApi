using Microsoft.EntityFrameworkCore;
using Rakt.BookingsService.Application;
using Rakt.BookingsService.Domain;
namespace Rakt.BookingsService.Infrastructure;
/// <summary>Контекст изолированной базы данных броней.</summary>
public sealed class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    /// <summary>Брони сервиса.</summary>
    public DbSet<Booking> Bookings => Set<Booking>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Booking>(e => { e.ToTable("bookings"); e.HasKey(x => x.Id); e.Property(x => x.Status).HasConversion<string>(); e.HasIndex(x => new { x.UserId, x.EventId }); });
    }
}

/// <summary>Создаёт контекст броней для инструментов EF Core.</summary>
public sealed class BookingsDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<BookingsDbContext>
{
    public BookingsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<BookingsDbContext>().UseNpgsql("Host=localhost;Database=rakt_bookings;Username=postgres;Password=postgres").Options);
}
/// <summary>Репозиторий броней EF Core.</summary>
public sealed class BookingRepository(BookingsDbContext db) : IBookingRepository
{
    public Task<Booking?> GetAsync(Guid id, CancellationToken ct = default) => db.Bookings.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddAsync(Booking booking, CancellationToken ct = default) => db.Bookings.AddAsync(booking, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
