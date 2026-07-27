using Microsoft.EntityFrameworkCore;
using Rakt.UsersService.Domain;
namespace Rakt.UsersService.Infrastructure;
/// <summary>Контекст изолированной базы данных пользователей.</summary>
public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    /// <summary>Пользователи сервиса.</summary>
    public DbSet<User> Users => Set<User>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Login).HasMaxLength(100).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(64).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>();
            entity.HasIndex(user => user.Login).IsUnique();
        });
    }
}
