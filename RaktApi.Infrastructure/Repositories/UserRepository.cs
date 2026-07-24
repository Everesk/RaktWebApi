using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RaktApi.Application.Ports;
using RaktApi.Domain;
using RaktApi.Infrastructure.Data;

namespace RaktApi.Infrastructure.Repositories;

/// <summary>Репозиторий для доступа к пользователям через EF Core.</summary>
public sealed class UserRepository(
    AppDbContext context,
    ILogger<UserRepository> logger) : IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Login == login &&
                        (user.Role == UserRole.User || user.Role == UserRole.Admin),
                cancellationToken);

        if (user is not null)
        {
            return user;
        }

        var role = await context.Database
            .SqlQueryRaw<string>("SELECT role AS \"Value\" FROM users WHERE login = {0}", login)
            .FirstOrDefaultAsync(cancellationToken);
        if (role is not null &&
            (!Enum.TryParse<UserRole>(role, out var parsedRole) || !Enum.IsDefined(parsedRole)))
        {
            logger.LogWarning("Пользователь с логином {Login} пропущен из-за некорректной роли {Role}", login, role);
        }

        return null;
    }

    /// <inheritdoc />
    public Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(user => user.Login == login, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
