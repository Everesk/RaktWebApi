using Microsoft.AspNetCore.Mvc;
using RaktWebApi.Models;
using RaktWebApi.Models.DTO;
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
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    /// <returns>Постраничный список событий.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<Event>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<Event>>> GetAll([FromQuery] EventQueryDto query, CancellationToken cancellationToken)
    {
        var events = await eventService.GetAllAsync(query, cancellationToken);

        logger.LogInformation("Запрошен список событий с параметрами: {@Query}", query);

        return Ok(events);
    }

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Event>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await eventService.GetByIdAsync(id, cancellationToken);

        logger.LogInformation("Запрошено событие с Id {Id}", entity.Id);

        return Ok(entity);
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Event>> Create([FromBody] CreateEventDto dto, CancellationToken cancellationToken)
    {
        var created = await eventService.CreateAsync(dto, cancellationToken);

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
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventDto dto, CancellationToken cancellationToken)
    {
        await eventService.UpdateAsync(id, dto, cancellationToken);

        logger.LogInformation("Обновлено событие с Id {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Удаляет событие.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await eventService.DeleteAsync(id, cancellationToken);

        logger.LogInformation("Удалено событие с Id {Id}", id);

        return NoContent();
    }

}
