using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Rakt.UsersService.Infrastructure;
/// <summary>Создаёт контекст пользователей для инструментов EF Core.</summary>
public sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql("Host=localhost;Database=rakt_users;Username=postgres;Password=postgres")
            .Options;

        return new UsersDbContext(options);
    }
}
