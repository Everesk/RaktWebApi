using RaktWebApi.Models;
using RaktWebApi.Models.DTO;

namespace RaktWebApi.Mappers;

/// <summary>
/// Методы расширения для преобразования Booking в DTO.
/// </summary>
public static class BookingMapper
{
    /// <summary>
    /// Преобразует Booking в BookingDto.
    /// </summary>
    public static BookingDto ToDto(this Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        return new BookingDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
    }
}
