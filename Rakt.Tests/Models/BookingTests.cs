using FluentAssertions;
using RaktWebApi.Models;

namespace Rakt.Tests.Models;

/// <summary>
/// Набор тестов для модели <see cref="Booking"/>.
/// </summary>
public class BookingTests
{
    /// <summary>
    /// Проверяет, что бронирование при создании получает корректные значения по умолчанию.
    /// </summary>
    [Fact]
    public void Booking_ShouldInitializeWithPendingStatusAndCurrentCreationTime()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var booking = CreateBooking(eventId);
        var afterCreate = DateTimeOffset.UtcNow;

        // Assert
        booking.Should().NotBeNull();
        booking.Id.Should().NotBe(Guid.Empty);
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
        booking.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        booking.CreatedAt.Should().BeOnOrBefore(afterCreate);
    }

    /// <summary>
    /// Создает экземпляр бронирования для тестов.
    /// </summary>
    private static Booking CreateBooking(Guid eventId)
    {
        return new Booking(eventId);
    }
}
