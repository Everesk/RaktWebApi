using FluentAssertions;
using RaktWebApi.Mappers;
using RaktWebApi.Models;

namespace Rakt.Tests.Mappers;

/// <summary>
/// Набор тестов для маппинга бронирований.
/// </summary>
public class BookingMapperTests
{
    /// <summary>
    /// Проверяет преобразование Booking в BookingDto.
    /// </summary>
    [Fact]
    public void ToDto_ShouldMapAllBookingFields()
    {
        // Arrange
        var booking = CreateBooking();

        // Act
        var dto = booking.ToDto();

        // Assert
        dto.Id.Should().Be(booking.Id);
        dto.EventId.Should().Be(booking.EventId);
        dto.Status.Should().Be(booking.Status);
        dto.CreatedAt.Should().Be(booking.CreatedAt);
        dto.ProcessedAt.Should().Be(booking.ProcessedAt);
    }

    /// <summary>
    /// Создает бронирование для тестов.
    /// </summary>
    private static Booking CreateBooking()
    {
        return new Booking(Guid.NewGuid());
    }
}
