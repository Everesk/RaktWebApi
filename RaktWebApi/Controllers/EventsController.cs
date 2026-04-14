using Microsoft.AspNetCore.Mvc;
using RaktWebApi.Models;
using RaktWebApi.Services;

namespace RaktWebApi.Controllers;

/// <summary>
/// Контроллер управления списком событий.
/// </summary>
[ApiController]
[Route("[controller]")]
public class EventsController(
    ILogger<EventsController> logger,
    IEventService eventService) : ApiControllerBase
{
    /// <summary>
    /// Возвращает список событий с учетом фильтрации и пагинации.
    /// </summary>
    /// <param name="query">Параметры фильтрации и пагинации событий.</param>
    /// <returns>Постраничный список событий.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<Event>), StatusCodes.Status200OK)]
    public ActionResult<PaginatedResult<Event>> GetAll([FromQuery] EventQueryDto query)
    {
        var events = eventService.GetAll(query);
        return Ok(events);
    }

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Event> GetById(Guid id)
    {
        var entity = eventService.GetById(id);

        return Ok(entity);
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Update(Guid id, [FromBody] UpdateEventDto dto)
    {
        eventService.Update(id, dto);

        logger.LogInformation("Обновлено событие с Id {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Удаляет событие.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        eventService.Delete(id);

        logger.LogInformation("Удалено событие с Id {Id}", id);

        return NoContent();
    }
}