using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RaktApi.Application.DTO;
using RaktApi.Application.Services;

namespace RaktWebApi.Controllers;

/// <summary>
/// Контроллер регистрации и аутентификации пользователей.
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AuthController(IUserService userService) : ApiControllerBase
{
    /// <summary>
    /// Регистрирует нового пользователя.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterUserDto dto, CancellationToken cancellationToken)
    {
        await userService.RegisterAsync(dto, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Проверяет учетные данные и возвращает JWT-токен.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthenticationDto>> Login(LoginDto dto, CancellationToken cancellationToken)
    {
        return Ok(await userService.LoginAsync(dto, cancellationToken));
    }
}
