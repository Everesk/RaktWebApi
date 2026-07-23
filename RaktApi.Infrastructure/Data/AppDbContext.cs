using Microsoft.EntityFrameworkCore;
using RaktApi.Domain;

namespace RaktApi.Infrastructure.Data;

/// <summary>
/// Контекст базы данных приложения.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Создает контекст базы данных с заданными параметрами подключения.
    /// </summary>
    /// <param name="options">Параметры конфигурации контекста.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Таблица событий.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>
    /// Таблица бронирований.
    /// </summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>
    /// Настраивает модель данных и автоматически подключает все конфигурации из сборки.
    /// </summary>
    /// <param name="modelBuilder">Построитель модели EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
