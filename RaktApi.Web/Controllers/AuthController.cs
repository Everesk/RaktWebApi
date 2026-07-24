using Microsoft.AspNetCore.Mvc;
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
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterUserDto dto, CancellationToken cancellationToken)
    {
        var user = await userService.RegisterAsync(dto, cancellationToken);
        return Created($"/users/{user.Id}", null);
    }

    /// <summary>
    /// Проверяет учетные данные и возвращает JWT-токен.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationDto>> Login(LoginDto dto, CancellationToken cancellationToken)
    {
        return Ok(await userService.LoginAsync(dto, cancellationToken));
    }
}
