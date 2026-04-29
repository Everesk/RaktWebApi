using Microsoft.AspNetCore.Mvc;
using RaktWebApi.Models;
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
    [ProducesResponseType(typeof(Booking), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Booking>> Create(Guid eventId)
    {
        var booking = await bookingService.CreateBookingAsync(eventId);

        logger.LogInformation("Создана бронь с Id {Id} для события {EventId}", booking.Id, booking.EventId);

        return AcceptedAtAction(
            actionName: nameof(GetById),
            routeValues: new { id = booking.Id },
            value: booking);
    }

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Booking>> GetById(Guid id)
    {
        var booking = await bookingService.GetBookingByIdAsync(id);

        logger.LogInformation("Запрошена бронь с Id {Id}", booking.Id);

        return Ok(booking);
    }
}
