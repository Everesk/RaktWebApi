using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rakt.UsersService.Application;
namespace Rakt.UsersService.Presentation.Controllers;
/// <summary>Контроллер регистрации и аутентификации пользователей.</summary>
[Route("auth")]
public sealed class AuthController(IUserService users) : ApiControllerBase
{
    /// <summary>Регистрирует пользователя и возвращает JWT-токен.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthenticationResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthenticationResult>> Register(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var result = await users.RegisterAsync(command, cancellationToken);
        return Created(string.Empty, result);
    }
    /// <summary>Проверяет учётные данные и возвращает JWT-токен.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationResult>> Login(LoginCommand command, CancellationToken cancellationToken) => Ok(await users.LoginAsync(command, cancellationToken));
}
