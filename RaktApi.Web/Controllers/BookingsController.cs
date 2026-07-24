using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RaktApi.Application.DTO;
using RaktApi.Application.Mappers;
using RaktApi.Application.Services;
using RaktApi.Domain;
using RaktWebApi.Extensions;

namespace RaktWebApi.Controllers;

/// <summary>
/// Контроллер для получения бронирований.
/// </summary>
[ApiController]
[Route("bookings")]
public class BookingsController(
    ILogger<BookingsController> logger,
    IBookingService bookingService) : ApiControllerBase
{
    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetById(Guid id, CancellationToken ct)
    {
        var booking = await bookingService.GetBookingByIdAsync(id, ct);
        var dto = booking.ToDto();

        logger.LogInformation("Запрошена бронь с Id {Id}", booking.Id);

        return Ok(dto);
    }

    /// <summary>
    /// Отменяет бронирование от имени указанного пользователя.
    /// </summary>
    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var userRole = User.IsInRole(nameof(UserRole.Admin)) ? UserRole.Admin : UserRole.User;
        await bookingService.CancelBookingAsync(id, userId, userRole, cancellationToken);

        logger.LogInformation("Отменена бронь с Id {Id} пользователем {UserId}", id, userId);

        return NoContent();
    }
}
