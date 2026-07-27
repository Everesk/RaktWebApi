using Microsoft.EntityFrameworkCore;
using Rakt.UsersService.Application;
using Rakt.UsersService.Domain;
namespace Rakt.UsersService.Infrastructure;
/// <summary>Репозиторий пользователей EF Core.</summary>
public sealed class UserRepository(UsersDbContext db) : IUserRepository
{
    public Task<User?> FindByLoginAsync(string login, CancellationToken ct = default) => db.Users.SingleOrDefaultAsync(x => x.Login == login, ct);
    public async Task AddAsync(User user, CancellationToken ct = default) { await db.Users.AddAsync(user, ct); await db.SaveChangesAsync(ct); }
}
