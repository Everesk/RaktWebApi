using FluentAssertions;
using RaktApi.Domain;

namespace Rakt.Tests.Models;

/// <summary>
/// Набор тестов для модели <see cref="Booking"/>.
/// </summary>
public class BookingTests
{
    /// <summary>
    /// Проверяет, что бронирование при создании получает корректные значения по умолчанию без установки даты создания (этим теперь EF перехватчик занимается).
    /// </summary>
    [Fact] public void Booking_ShouldInitializeWithPendingStatusAndNullCreationTime()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var booking = CreateBooking(eventId);

        // Assert
        booking.Should().NotBeNull();
        booking.Id.Should().NotBe(Guid.Empty);
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
        booking.CreatedAt.Should().Be(default);
    }

    /// <summary>
    /// Создает экземпляр бронирования для тестов.
    /// </summary>
    private static Booking CreateBooking(Guid eventId)
    {
        return new Booking(eventId);
    }
}
