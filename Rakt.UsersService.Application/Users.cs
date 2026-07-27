using Rakt.UsersService.Domain;

namespace Rakt.UsersService.Application;

/// <summary>Данные регистрации пользователя.</summary>
public sealed record RegisterUserCommand(string Login, string Password, UserRole Role = UserRole.User);
/// <summary>Данные входа пользователя.</summary>
public sealed record LoginCommand(string Login, string Password);
/// <summary>Результат аутентификации.</summary>
public sealed record AuthenticationResult(Guid UserId, string Token);

/// <summary>Порт хранения пользователей.</summary>
public interface IUserRepository { Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken = default); Task AddAsync(User user, CancellationToken cancellationToken = default); }
/// <summary>Порт хеширования и проверки паролей.</summary>
public interface IPasswordHasher { string Hash(string password); bool Verify(string password, string hash); }
/// <summary>Порт выпуска JWT.</summary>
public interface IJwtTokenGenerator { string Generate(User user); }
/// <summary>Сценарии регистрации и входа.</summary>
public interface IUserService { Task<AuthenticationResult> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default); Task<AuthenticationResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default); }

/// <summary>Реализация сценариев сервиса пользователей.</summary>
public sealed class UserService(IUserRepository users, IPasswordHasher passwords, IJwtTokenGenerator tokens) : IUserService
{
    /// <summary>Регистрирует уникального пользователя и выдаёт ему токен.</summary>
    public async Task<AuthenticationResult> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        if (await users.FindByLoginAsync(command.Login, cancellationToken) is not null) throw new InvalidOperationException("Пользователь с таким логином уже существует.");
        var user = User.Create(command.Login, passwords.Hash(command.Password), command.Role);
        await users.AddAsync(user, cancellationToken);
        return new(user.Id, tokens.Generate(user));
    }
    /// <summary>Проверяет пароль и выдаёт JWT.</summary>
    public async Task<AuthenticationResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByLoginAsync(command.Login, cancellationToken);
        if (user is null || !passwords.Verify(command.Password, user.PasswordHash)) throw new UnauthorizedAccessException("Неверный логин или пароль.");
        return new(user.Id, tokens.Generate(user));
    }
}
