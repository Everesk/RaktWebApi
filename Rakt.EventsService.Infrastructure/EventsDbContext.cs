using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Domain;
namespace Rakt.EventsService.Infrastructure;
/// <summary>Контекст изолированной базы данных событий.</summary>
public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    /// <summary>События сервиса.</summary>
    public DbSet<Event> Events => Set<Event>();
    protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<Event>(e => { e.ToTable("events"); e.HasKey(x => x.Id); e.Property(x => x.Title).HasMaxLength(200).IsRequired(); });
}
