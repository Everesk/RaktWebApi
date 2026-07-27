namespace Rakt.UsersService.Application;
/// <summary>Результат аутентификации.</summary>
public sealed record AuthenticationResult(Guid UserId, string Token);
