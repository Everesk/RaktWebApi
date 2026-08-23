using System.ComponentModel.DataAnnotations;

namespace Rakt.BookingsService.Presentation.Controllers;

/// <summary>
/// Содержит идентификатор события для создания брони.
/// </summary>
public sealed class CreateBookingRequest
{
    /// <summary>
    /// Идентификатор бронируемого события.
    /// </summary>
    [Required]
    public Guid EventId { get; init; }
}
