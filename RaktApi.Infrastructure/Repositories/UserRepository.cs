using Microsoft.EntityFrameworkCore;
using RaktApi.Application.Ports;
using RaktApi.Domain;
using RaktApi.Infrastructure.Data;

namespace RaktApi.Infrastructure.Repositories;

/// <summary>Репозиторий для доступа к пользователям через EF Core.</summary>
public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    /// <inheritdoc />
    public Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default) =>
        context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Login == login, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
