using Microsoft.AspNetCore.Mvc;

namespace Rakt.UsersService.Presentation.Controllers;

/// <summary>
/// Проверяет доступность HTTP-сервиса пользователей.
/// </summary>
[Route("health")]
public sealed class HealthController : ApiControllerBase
{
    /// <summary>
    /// Возвращает признак доступности HTTP-сервиса.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok();
    }
}
