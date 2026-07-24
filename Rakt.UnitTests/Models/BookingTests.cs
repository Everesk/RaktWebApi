using FluentAssertions;
using RaktApi.Domain;
using RaktApi.Domain.Exceptions;

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
        var userId = Guid.NewGuid();

        // Act
        var booking = CreateBooking(eventId, userId);

        // Assert
        booking.Should().NotBeNull();
        booking.Id.Should().NotBe(Guid.Empty);
        booking.EventId.Should().Be(eventId);
        booking.UserId.Should().Be(userId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
        booking.CreatedAt.Should().Be(default);
    }

    /// <summary>
    /// Проверяет перевод бронирования в статус отмены.
    /// </summary>
    [Fact]
    public void Cancel_ShouldSetCancelledStatus()
    {
        // Arrange
        var booking = CreateBooking(Guid.NewGuid(), Guid.NewGuid());

        // Act
        booking.Cancel();

        // Assert
        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    /// <summary>
    /// Проверяет запрет повторной отмены бронирования.
    /// </summary>
    [Fact]
    public void Cancel_ShouldThrowException_WhenBookingIsAlreadyCancelled()
    {
        // Arrange
        var booking = CreateBooking(Guid.NewGuid(), Guid.NewGuid());
        booking.Cancel();

        // Act
        Action act = booking.Cancel;

        // Assert
        act.Should().Throw<BookingAlreadyCancelledException>();
    }

    /// <summary>
    /// Создает экземпляр бронирования для тестов.
    /// </summary>
    private static Booking CreateBooking(Guid eventId, Guid userId)
    {
        return new Booking(eventId, userId);
    }
}
