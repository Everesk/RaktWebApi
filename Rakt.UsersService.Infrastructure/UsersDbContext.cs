using Microsoft.EntityFrameworkCore;
using Rakt.UsersService.Domain;
namespace Rakt.UsersService.Infrastructure;
/// <summary>Контекст изолированной базы данных пользователей.</summary>
public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    /// <summary>Пользователи сервиса.</summary>
    public DbSet<User> Users => Set<User>();
    protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<User>(e => { e.ToTable("users"); e.HasKey(x => x.Id); e.Property(x => x.Login).HasMaxLength(100).IsRequired(); e.Property(x => x.PasswordHash).HasMaxLength(64).IsRequired(); e.Property(x => x.Role).HasConversion<string>(); e.HasIndex(x => x.Login).IsUnique(); });
}
