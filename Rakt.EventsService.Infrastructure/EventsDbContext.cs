using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Domain;
namespace Rakt.EventsService.Infrastructure;
/// <summary>Контекст изолированной базы данных событий.</summary>
public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    /// <summary>События сервиса.</summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>Состояния резервирования мест по броням.</summary>
    public DbSet<BookingSeatReservation> BookingSeatReservations => Set<BookingSeatReservation>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Event>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(eventEntity => eventEntity.Id);
            entity.Property(eventEntity => eventEntity.Title).HasMaxLength(200).IsRequired();
        });

        builder.Entity<BookingSeatReservation>(entity =>
        {
            entity.ToTable("booking_seat_reservations");
            entity.HasKey(reservation => reservation.BookingId);
        });
    }
}
