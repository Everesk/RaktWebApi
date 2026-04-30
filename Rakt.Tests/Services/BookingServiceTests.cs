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
    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute, int second)
    {
        return new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
    }

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
        booking.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Проверяет, что для одного события можно создать несколько броней с уникальными идентификаторами.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldCreateMultipleBookingsWithUniqueIds()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateEvent();
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);

        // Act
        var first = await service.CreateBookingAsync(eventEntity.Id);
        var second = await service.CreateBookingAsync(eventEntity.Id);

        // Assert
        first.Id.Should().NotBe(second.Id);
        first.EventId.Should().Be(eventEntity.Id);
        second.EventId.Should().Be(eventEntity.Id);
        first.Status.Should().Be(BookingStatus.Pending);
        second.Status.Should().Be(BookingStatus.Pending);
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
    /// Проверяет, что сервис возвращает актуальный статус бронирования после изменения состояния.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_ShouldReflectStatusChange()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateEvent();
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);
        var created = await service.CreateBookingAsync(eventEntity.Id);

        created.Confirm(DateTimeOffset.UtcNow);

        // Act
        var booking = await service.GetBookingByIdAsync(created.Id);

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Проверяет, что сервис возвращает актуальный статус бронирования после отклонения.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_ShouldReflectRejectedStatus()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateEvent();
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);
        var created = await service.CreateBookingAsync(eventEntity.Id);

        created.Reject(DateTimeOffset.UtcNow);

        // Act
        var booking = await service.GetBookingByIdAsync(created.Id);

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().NotBeNull();
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
    /// Проверяет, что сервис не создает бронь для удаленного события.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventWasDeleted()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateEvent();
        eventRepository.Add(eventEntity);
        eventRepository.Delete(eventEntity);
        var service = CreateService(eventRepository);

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventEntity.Id);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{eventEntity.Id}*");
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
            startAt: Utc(2026, 4, 1, 10, 0, 0),
            endAt: Utc(2026, 4, 1, 11, 0, 0));
    }
}
