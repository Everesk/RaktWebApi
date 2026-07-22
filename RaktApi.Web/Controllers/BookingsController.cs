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
