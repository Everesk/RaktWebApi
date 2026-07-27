using Rakt.UsersService.Domain;
using Rakt.UsersService.Domain.Exceptions;
namespace Rakt.UsersService.Application;
/// <summary>Реализация сценариев сервиса пользователей.</summary>
public sealed class UserService(IUserRepository users, IPasswordHasher passwords, IJwtTokenGenerator tokens) : IUserService
{
    /// <summary>Регистрирует уникального пользователя и выдаёт ему токен.</summary>
    public async Task<AuthenticationResult> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        if (await users.FindByLoginAsync(command.Login, cancellationToken) is not null)
        {
            throw new UserAlreadyExistsException(
                $"Пользователь с логином '{command.Login}' уже существует.");
        }

        var user = User.Create(command.Login, passwords.Hash(command.Password), command.Role);

        await users.AddAsync(user, cancellationToken);

        return new(user.Id, tokens.Generate(user));
    }
    /// <summary>Проверяет пароль и выдаёт JWT.</summary>
    public async Task<AuthenticationResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByLoginAsync(command.Login, cancellationToken);
        if (user is null || !passwords.Verify(command.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException("Неверный логин или пароль.");
        }

        return new(user.Id, tokens.Generate(user));
    }
}
