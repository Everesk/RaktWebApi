using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Rakt.EventsService.Infrastructure;
/// <summary>Создаёт контекст событий для инструментов EF Core.</summary>
public sealed class EventsDbContextFactory : IDesignTimeDbContextFactory<EventsDbContext>
{
    public EventsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<EventsDbContext>().UseNpgsql("Host=localhost;Database=rakt_events;Username=postgres;Password=postgres").Options);
}
