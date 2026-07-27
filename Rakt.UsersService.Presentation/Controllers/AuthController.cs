using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rakt.UsersService.Application;
namespace Rakt.UsersService.Presentation.Controllers;
/// <summary>Контроллер регистрации и аутентификации пользователей.</summary>
[Route("auth")]
public sealed class AuthController(IUserService users) : ApiControllerBase
{
    /// <summary>Регистрирует пользователя.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Register(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        await users.RegisterAsync(command, cancellationToken);

        return NoContent();
    }
    /// <summary>Проверяет учётные данные и возвращает JWT-токен.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationResult>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await users.LoginAsync(command, cancellationToken);

        return Ok(result);
    }
}
