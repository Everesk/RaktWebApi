using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rakt.BookingsService.Application;
using Rakt.BookingsService.Domain;

namespace Rakt.BookingsService.Presentation.Controllers;

/// <summary>
/// Предоставляет HTTP-операции создания и отмены броней.
/// </summary>
[Authorize]
[Route("bookings")]
public sealed class BookingsController(BookingService bookings) : ApiControllerBase
{
    /// <summary>
    /// Возвращает бронь по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор брони.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Booking>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetAsync(id, cancellationToken);

        return Ok(booking);
    }

    /// <summary>
    /// Возвращает брони указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    [HttpGet("by-event/{eventId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<Booking>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<Booking>>> GetByEventId(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var result = await bookings.GetByEventIdAsync(eventId, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Создаёт бронь от имени пользователя из JWT-токена.
    /// </summary>
    /// <param name="request">Данные создаваемой брони.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    [HttpPost]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Booking>> Create(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var booking = await bookings.CreateAsync(userId, request.EventId, cancellationToken);

        return AcceptedAtAction(nameof(Cancel), new { id = booking.Id }, booking);
    }

    /// <summary>
    /// Отменяет бронь. Доступна только аутентифицированному пользователю.
    /// </summary>
    /// <param name="id">Идентификатор брони.</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await bookings.CancelAsync(id, userId, User.IsInRole("Admin"), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Извлекает идентификатор пользователя из обязательного subject claim JWT-токена.
    /// </summary>
    private bool TryGetUserId(out Guid userId)
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(subject, out userId);
    }
}
