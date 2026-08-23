using Microsoft.AspNetCore.Mvc;
namespace Rakt.EventsService.Presentation.Controllers;
/// <summary>Контроллер проверки доступности сервиса событий.</summary>
[Route("health")]
public sealed class HealthController : ApiControllerBase
{
    /// <summary>Возвращает признак доступности HTTP-сервиса.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok();
}
