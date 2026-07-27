namespace Rakt.UsersService.Application;
/// <summary>Сценарии регистрации и входа.</summary>
public interface IUserService
{
    Task<AuthenticationResult> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default);

    Task<AuthenticationResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
}
