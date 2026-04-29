using FluentAssertions;
using RaktWebApi.Common.Exceptions;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;
using RaktWebApi.Services;

namespace Rakt.Tests.Services;

/// <summary>
/// Набор тестов для сервиса <see cref="BookingService"/>.
/// </summary>
public class BookingServiceTests
{
    /// <summary>
    /// Проверяет, что сервис создает бронирование с ожидаемыми значениями по умолчанию.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldCreatePendingBooking()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateEvent();
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);

        // Act
        var booking = await service.CreateBookingAsync(eventEntity.Id);

        // Assert
        booking.Should().NotBeNull();
        booking.Id.Should().NotBe(Guid.Empty);
        booking.EventId.Should().Be(eventEntity.Id);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
        booking.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Проверяет, что сервис возвращает бронирование по идентификатору.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenBookingExists()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateEvent();
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);
        var created = await service.CreateBookingAsync(eventEntity.Id);

        // Act
        var booking = await service.GetBookingByIdAsync(created.Id);

        // Assert
        booking.Should().NotBeNull();
        booking.Id.Should().Be(created.Id);
        booking.EventId.Should().Be(eventEntity.Id);
    }

    /// <summary>
    /// Проверяет, что сервис бросает исключение, если бронь не найдена.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        // Arrange
        var service = CreateService(new InMemoryEventRepository());
        var bookingId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await service.GetBookingByIdAsync(bookingId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{bookingId}*");
    }

    /// <summary>
    /// Проверяет, что сервис не создает бронь для несуществующего события.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var service = CreateService(new InMemoryEventRepository());
        var eventId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{eventId}*");
    }

    /// <summary>
    /// Создает экземпляр сервиса для тестов.
    /// </summary>
    private static BookingService CreateService(InMemoryEventRepository eventRepository)
    {
        var bookingRepository = new InMemoryBookingRepository();
        return new BookingService(bookingRepository, eventRepository);
    }

    /// <summary>
    /// Создает событие для тестов.
    /// </summary>
    private static Event CreateEvent()
    {
        return new Event(
            title: "Тестовое событие",
            description: null,
            startAt: new DateTime(2026, 4, 1, 10, 0, 0),
            endAt: new DateTime(2026, 4, 1, 11, 0, 0));
    }
}
