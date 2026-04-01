using Microsoft.AspNetCore.Mvc;
using RaktWebApi.Models;
using RaktWebApi.Services;

namespace RaktWebApi.Controllers;

/// <summary>
/// Контроллер управления списком событий
/// </summary>

[ApiController]
[Route("[controller]")]
public class EventsController(ILogger<EventsController> logger, IEventService eventService) : ApiControllerBase
{
    /// <summary>
    /// Возвращает список всех событий.
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<Event>> GetAll() => Ok(eventService.GetAll());

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    [HttpGet("{id:guid}")]
    public ActionResult<Event> GetById(Guid id)
    {
        var entity = eventService.GetById(id);

        if (entity is null) return NotFoundProblem($"Событие с идентификатором '{id}' не найдено.");

        return Ok(entity);
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    [HttpPost]
    public ActionResult<Event> Create([FromBody] CreateEventDto dto)
    {
        var created = eventService.Create(dto);

        logger.LogInformation("Создано событие с Id {Id}", created.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] UpdateEventDto dto)
    {
        var updated = eventService.Update(id, dto);

        if (!updated) return NotFoundProblem($"Событие с идентификатором '{id}' не найдено.");

        logger.LogInformation("Обновлено событие с Id {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Удаляет событие.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var deleted = eventService.Delete(id);

        if (!deleted) return NotFoundProblem($"Событие с идентификатором '{id}' не найдено.");

        logger.LogInformation("Удалено событие с Id {Id}", id);

        return NoContent();
    }
}