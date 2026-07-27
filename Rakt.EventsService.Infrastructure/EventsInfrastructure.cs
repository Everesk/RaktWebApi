using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Application;
using Rakt.EventsService.Domain;
namespace Rakt.EventsService.Infrastructure;
/// <summary>Контекст изолированной базы данных событий.</summary>
public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    /// <summary>События сервиса.</summary>
    public DbSet<Event> Events => Set<Event>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Event>(e => { e.ToTable("events"); e.HasKey(x => x.Id); e.Property(x => x.Title).HasMaxLength(200).IsRequired(); });
    }
}

/// <summary>Создаёт контекст событий для инструментов EF Core.</summary>
public sealed class EventsDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<EventsDbContext>
{
    public EventsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<EventsDbContext>().UseNpgsql("Host=localhost;Database=rakt_events;Username=postgres;Password=postgres").Options);
}
/// <summary>Репозиторий событий EF Core.</summary>
public sealed class EventRepository(EventsDbContext db) : IEventRepository
{
    public Task<Event?> GetAsync(Guid id, CancellationToken ct = default) => db.Events.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task AddAsync(Event entity, CancellationToken ct = default) => db.Events.AddAsync(entity, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
