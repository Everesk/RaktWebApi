using Microsoft.AspNetCore.Mvc;

namespace RaktWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(ILogger<EventsController> logger) : ControllerBase
{
    private readonly ILogger<EventsController> _logger = logger;


}
