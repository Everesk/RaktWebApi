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
        var eventEntity = CreateTestEvent();
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
        eventRepository.GetById(eventEntity.Id)!.AvailableSeats.Should().Be(9);
    }

    /// <summary>
    /// Проверяет, что для одного события можно создать несколько броней с уникальными идентификаторами.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldCreateMultipleBookingsWithUniqueIds()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateTestEvent(totalSeats: 3);
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);

        // Act
        var bookings = new[]
        {
            await service.CreateBookingAsync(eventEntity.Id),
            await service.CreateBookingAsync(eventEntity.Id),
            await service.CreateBookingAsync(eventEntity.Id)
        };

        // Assert
        bookings.Select(x => x.Id).Should().OnlyHaveUniqueItems();
        bookings.Should().OnlyContain(x => x.EventId == eventEntity.Id);
        bookings.Should().OnlyContain(x => x.Status == BookingStatus.Pending);
        eventRepository.GetById(eventEntity.Id)!.AvailableSeats.Should().Be(0);
    }

    /// <summary>
    /// Проверяет, что сервис возвращает бронирование по идентификатору.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenBookingExists()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateTestEvent();
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
    /// Проверяет, что сервис возвращает только брони выбранного события.
    /// </summary>
    [Fact]
    public async Task GetBookingsByEventIdAsync_ShouldReturnOnlyEventBookings()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var firstEvent = CreateTestEvent();
        var secondEvent = CreateTestEvent();
        eventRepository.Add(firstEvent);
        eventRepository.Add(secondEvent);
        var service = CreateService(eventRepository);

        var firstBooking = await service.CreateBookingAsync(firstEvent.Id);
        var secondBooking = await service.CreateBookingAsync(firstEvent.Id);
        await service.CreateBookingAsync(secondEvent.Id);

        // Act
        var bookings = await service.GetBookingsByEventIdAsync(firstEvent.Id);

        // Assert
        bookings.Should().HaveCount(2);
        bookings.Select(x => x.Id).Should().BeEquivalentTo([firstBooking.Id, secondBooking.Id]);
        bookings.Should().OnlyContain(x => x.EventId == firstEvent.Id);
    }

    /// <summary>
    /// Проверяет, что сервис не возвращает список броней для несуществующего события.
    /// </summary>
    [Fact]
    public async Task GetBookingsByEventIdAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        // Arrange
        var service = CreateService(new InMemoryEventRepository());
        var eventId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await service.GetBookingsByEventIdAsync(eventId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{eventId}*");
    }

    /// <summary>
    /// Проверяет, что сервис возвращает актуальный статус бронирования после изменения состояния.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_ShouldReflectStatusChange()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateTestEvent();
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
        var eventEntity = CreateTestEvent();
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
    /// Проверяет, что после отклонения брони можно вернуть место в пул события.
    /// </summary>
    [Fact]
    public async Task ReleaseSeats_ShouldRestoreAvailableSeats_AfterBookingRejected()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateTestEvent(totalSeats: 1);
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);
        var created = await service.CreateBookingAsync(eventEntity.Id);

        // Act
        created.Reject(DateTimeOffset.UtcNow);
        eventEntity.ReleaseSeats();

        // Assert
        created.Status.Should().Be(BookingStatus.Rejected);
        created.ProcessedAt.Should().NotBeNull();
        eventRepository.GetById(eventEntity.Id)!.AvailableSeats.Should().Be(1);
    }

    /// <summary>
    /// Проверяет, что после отклонения и возврата места можно создать новую бронь.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldCreateNewBooking_AfterRejectedBookingReleasesSeat()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateTestEvent(totalSeats: 1);
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);
        var rejectedBooking = await service.CreateBookingAsync(eventEntity.Id);

        rejectedBooking.Reject(DateTimeOffset.UtcNow);
        eventEntity.ReleaseSeats();

        // Act
        var newBooking = await service.CreateBookingAsync(eventEntity.Id);

        // Assert
        newBooking.Id.Should().NotBe(rejectedBooking.Id);
        newBooking.Status.Should().Be(BookingStatus.Pending);
        eventRepository.GetById(eventEntity.Id)!.AvailableSeats.Should().Be(0);
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
        var eventEntity = CreateTestEvent();
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
    /// Проверяет, что сервис не создает бронь, если свободные места закончились.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenNoSeatsAvailable()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateTestEvent(totalSeats: 1);
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);

        await service.CreateBookingAsync(eventEntity.Id);

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventEntity.Id);

        // Assert
        await act.Should().ThrowAsync<NoAvailableSeatsException>();
        eventRepository.GetById(eventEntity.Id)!.AvailableSeats.Should().Be(0);
    }

    /// <summary>
    /// Проверяет, что конкурентные запросы не создают броней больше, чем доступно мест.
    /// </summary>
    private const int OverbookingTotalSeats = 5;
    private const int OverbookingRequestCount = 20;

    [Theory]
    [InlineData(OverbookingTotalSeats, OverbookingRequestCount)]
    public async Task CreateBookingAsync_ShouldNotOverbook_WhenRequestsAreConcurrent(int totalSeats, int requestCount)
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateTestEvent(totalSeats: totalSeats);
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);

        // Act
        var attempts = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await service.CreateBookingAsync(eventEntity.Id);
                    return (Success: true, Exception: (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (Success: false, Exception: ex);
                }
            }));

        var results = await Task.WhenAll(attempts);

        // Assert
        results.Count(x => x.Success).Should().Be(totalSeats);
        results.Count(x => x.Exception is NoAvailableSeatsException).Should().Be(requestCount - totalSeats);
        eventRepository.GetById(eventEntity.Id)!.AvailableSeats.Should().Be(0);
    }

    /// <summary>
    /// Проверяет, что разные экземпляры сервиса используют общую блокировку и не создают овербукинг.
    /// </summary>
    [Theory]
    [InlineData(OverbookingTotalSeats, OverbookingRequestCount)]
    public async Task CreateBookingAsync_ShouldNotOverbook_WhenDifferentServiceInstancesAreConcurrent(int totalSeats, int requestCount)
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var bookingRepository = new InMemoryBookingRepository();
        var eventEntity = CreateTestEvent(totalSeats: totalSeats);
        eventRepository.Add(eventEntity);

        // Act
        var attempts = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(async () =>
            {
                var service = new BookingService(bookingRepository, eventRepository);

                try
                {
                    await service.CreateBookingAsync(eventEntity.Id);
                    return (Success: true, Exception: (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (Success: false, Exception: ex);
                }
            }));

        var results = await Task.WhenAll(attempts);

        // Assert
        results.Count(x => x.Success).Should().Be(totalSeats);
        results.Count(x => x.Exception is NoAvailableSeatsException).Should().Be(requestCount - totalSeats);
        bookingRepository.GetAll().Should().HaveCount(totalSeats);
        eventRepository.GetById(eventEntity.Id)!.AvailableSeats.Should().Be(0);
    }

    /// <summary>
    /// Проверяет, что конкурентные успешные запросы создают брони с уникальными идентификаторами.
    /// </summary>
    private const int UniqueIdsTotalSeats = 10;
    private const int UniqueIdsRequestCount = 10;

    [Theory]
    [InlineData(UniqueIdsTotalSeats, UniqueIdsRequestCount)]
    public async Task CreateBookingAsync_ShouldCreateUniqueIds_WhenConcurrentRequestsFitSeatsLimit(int totalSeats, int requestCount)
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateTestEvent(totalSeats: totalSeats);
        eventRepository.Add(eventEntity);
        var service = CreateService(eventRepository);

        // Act
        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(() => service.CreateBookingAsync(eventEntity.Id)));

        var bookings = await Task.WhenAll(tasks);

        // Assert
        bookings.Should().HaveCount(requestCount);
        bookings.Select(x => x.Id).Should().OnlyHaveUniqueItems();
        eventRepository.GetById(eventEntity.Id)!.AvailableSeats.Should().Be(totalSeats - requestCount);
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
    private static Event CreateTestEvent(int totalSeats = 10)
    {
        return new Event(
            title: "Тестовое событие",
            description: null,
            startAt: Utc(2026, 4, 1, 10, 0, 0),
            endAt: Utc(2026, 4, 1, 11, 0, 0),
            totalSeats: totalSeats);
    }
}
