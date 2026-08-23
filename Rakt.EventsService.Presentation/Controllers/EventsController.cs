using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rakt.EventsService.Application;

namespace Rakt.EventsService.Presentation.Controllers;

/// <summary>
/// Предоставляет HTTP-операции управления событиями.
/// </summary>
[Route("events")]
public sealed class EventsController(IEventService events) : ApiControllerBase
{
    /// <summary>
    /// Возвращает список событий с фильтрацией и пагинацией.
    /// </summary>
    /// <param name="query">Параметры запроса списка событий.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<EventInfoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<EventInfoDto>>> GetAll(
        [FromQuery] EventQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await events.GetAllAsync(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Возвращает десять событий с наибольшей долей проданных мест.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    [HttpGet("top")]
    [ProducesResponseType(typeof(IReadOnlyList<EventInfoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventInfoDto>>> GetTop(CancellationToken cancellationToken)
    {
        var result = await events.GetTopAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventInfoDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await events.GetByIdAsync(id, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Создаёт новое событие. Доступно только администратору.
    /// </summary>
    /// <param name="request">Данные нового события.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(EventInfoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<EventInfoDto>> Create(
        [FromBody] CreateEventDto request,
        CancellationToken cancellationToken)
    {
        var result = await events.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Обновляет событие. Доступно только администратору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="request">Новые данные события.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEventDto request,
        CancellationToken cancellationToken)
    {
        await events.UpdateAsync(id, request, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Удаляет событие. Доступно только администратору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await events.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

}
