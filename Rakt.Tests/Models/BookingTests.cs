using FluentAssertions;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;

namespace Rakt.Tests.Models;

/// <summary>
/// Набор тестов для модели <see cref="Booking"/> и ее хранилища.
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
        var beforeCreate = DateTime.Now;

        // Act
        var booking = CreateBooking(eventId);
        var afterCreate = DateTime.Now;

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
    /// Проверяет работу in-memory хранилища бронирований.
    /// </summary>
    [Fact]
    public void InMemoryBookingRepository_ShouldStoreAndReturnBookings()
    {
        // Arrange
        var repository = new InMemoryBookingRepository();
        var booking = CreateBooking(Guid.NewGuid());

        // Act
        repository.Add(booking);
        var stored = repository.GetById(booking.Id);
        var all = repository.GetAll();

        // Assert
        stored.Should().NotBeNull();
        stored.Should().BeSameAs(booking);
        all.Should().ContainSingle();
        all.Should().ContainSingle(x => x.Id == booking.Id);
    }

    /// <summary>
    /// Проверяет, что бронирование можно удалить из хранилища.
    /// </summary>
    [Fact]
    public void InMemoryBookingRepository_ShouldDeleteBooking()
    {
        // Arrange
        var repository = new InMemoryBookingRepository();
        var booking = CreateBooking(Guid.NewGuid());
        repository.Add(booking);

        // Act
        repository.Delete(booking);

        // Assert
        repository.GetAll().Should().BeEmpty();
        repository.GetById(booking.Id).Should().BeNull();
    }

    /// <summary>
    /// Создает экземпляр бронирования для тестов.
    /// </summary>
    private static Booking CreateBooking(Guid eventId)
    {
        return new Booking(eventId);
    }
}
