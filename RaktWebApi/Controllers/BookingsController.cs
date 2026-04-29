using Microsoft.AspNetCore.Mvc;
using RaktWebApi.Mappers;
using RaktWebApi.Models.DTO;
using RaktWebApi.Services;

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
    /// Создает бронь для указанного события.
    /// </summary>
    [HttpPost("{eventId:guid}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> Create(Guid eventId, CancellationToken ct)
    {
        var booking = await bookingService.CreateBookingAsync(eventId, ct);
        var dto = booking.ToDto();

        logger.LogInformation("Создана бронь с Id {Id} для события {EventId}", booking.Id, booking.EventId);

        return AcceptedAtAction(
            actionName: nameof(GetById),
            routeValues: new { id = booking.Id },
            value: dto);
    }

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
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
}
