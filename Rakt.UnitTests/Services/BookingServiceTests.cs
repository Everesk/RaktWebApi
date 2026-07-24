using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaktApi.Infrastructure.Data.Interceptors;
using RaktApi.Domain;
using RaktApi.Domain.Exceptions;
using RaktApi.Infrastructure.Data;
using RaktApi.Application.Ports;
using RaktApi.Application.Services;
using RaktApi.Infrastructure.Repositories;
using Rakt.Tests.Infrastructure;

namespace Rakt.Tests.Services;

/// <summary>
/// Набор тестов для сервиса <see cref="BookingService"/>.
/// </summary>
public class BookingServiceTests : InMemoryDbTestBase
{
    private const int OverbookingTotalSeats = 5;
    private const int OverbookingRequestCount = 20;

    /// <summary>
    /// Настраивает сервисы, необходимые для тестов <see cref="BookingService"/>.
    /// </summary>
    /// <param name="services">Коллекция сервисов DI.</param>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
    }

    /// <summary>
    /// Настраивает DbContext для тестов сервиса бронирований.
    /// </summary>
    /// <param name="services">Коллекция сервисов DI.</param>
    protected override void ConfigureDbContext(IServiceCollection services)
    {
        services.AddSingleton<BookingCreatedAtInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(DatabaseName);
            options.AddInterceptors(sp.GetRequiredService<BookingCreatedAtInterceptor>());
        });
    }

    /// <summary>
    /// Проверяет, что сервис создает бронирование с ожидаемыми значениями по умолчанию.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldCreatePendingBooking()
    {
        // Arrange
        var eventEntity = await SeedEventAsync();
        var userId = Guid.NewGuid();
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;

        // Act
        var booking = await service.CreateBookingAsync(eventEntity.Id, userId);

        // Assert
        booking.Should().NotBeNull();
        booking.Id.Should().NotBe(Guid.Empty);
        booking.EventId.Should().Be(eventEntity.Id);
        booking.UserId.Should().Be(userId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
        booking.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));

        using var verificationScope = CreateScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedEvent = await context.Events.AsNoTracking().FirstAsync(x => x.Id == eventEntity.Id);
        storedEvent.AvailableSeats.Should().Be(9);
    }

    /// <summary>
    /// Проверяет запрет создания бронирования для уже начавшегося события.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldThrowPastEventBookingException_WhenEventHasStarted()
    {
        // Arrange
        var eventEntity = await SeedEventAsync(startAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<PastEventBookingException>();
    }

    /// <summary>
    /// Проверяет запрет создания более десяти активных бронирований одним пользователем.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldThrowActiveBookingsLimitExceededException_WhenLimitIsReached()
    {
        // Arrange
        var eventEntity = await SeedEventAsync(totalSeats: 11);
        var userId = Guid.NewGuid();
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;

        for (var index = 0; index < 10; index++)
        {
            await service.CreateBookingAsync(eventEntity.Id, userId);
        }

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventEntity.Id, userId);

        // Assert
        await act.Should().ThrowAsync<ActiveBookingsLimitExceededException>();
    }

    /// <summary>
    /// Проверяет, что владелец может отменить свою бронь и место освобождается.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ShouldCancelOwnBookingAndReleaseSeat()
    {
        // Arrange
        var eventEntity = await SeedEventAsync(totalSeats: 1);
        var userId = Guid.NewGuid();
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;
        var booking = await service.CreateBookingAsync(eventEntity.Id, userId);

        // Act
        await service.CancelBookingAsync(booking.Id, userId, UserRole.User);

        // Assert
        var cancelledBooking = await service.GetBookingByIdAsync(booking.Id);
        cancelledBooking.Status.Should().Be(BookingStatus.Cancelled);

        using var verificationScope = CreateScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedEvent = await context.Events.AsNoTracking().FirstAsync(x => x.Id == eventEntity.Id);
        storedEvent.AvailableSeats.Should().Be(1);
    }

    /// <summary>
    /// Проверяет, что администратор может отменить чужую бронь.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ShouldAllowAdministratorToCancelAnotherUsersBooking()
    {
        // Arrange
        var eventEntity = await SeedEventAsync();
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;
        var booking = await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());

        // Act
        await service.CancelBookingAsync(booking.Id, Guid.NewGuid(), UserRole.Admin);

        // Assert
        var cancelledBooking = await service.GetBookingByIdAsync(booking.Id);
        cancelledBooking.Status.Should().Be(BookingStatus.Cancelled);
    }

    /// <summary>
    /// Проверяет запрет отмены чужой брони обычным пользователем.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ShouldThrowOperationForbiddenException_WhenUserDoesNotOwnBooking()
    {
        // Arrange
        var eventEntity = await SeedEventAsync();
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;
        var booking = await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());

        // Act
        Func<Task> act = async () => await service.CancelBookingAsync(booking.Id, Guid.NewGuid(), UserRole.User);

        // Assert
        await act.Should().ThrowAsync<OperationForbiddenException>();
    }

    /// <summary>
    /// Проверяет, что перехватчик EF Core заполняет дату создания при сохранении новой брони.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_ShouldSetCreatedAtForNewBooking()
    {
        // Arrange
        var eventEntity = await SeedEventAsync();
        var beforeSave = DateTimeOffset.UtcNow;

        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = new Booking(eventEntity.Id, Guid.NewGuid());

        booking.CreatedAt.Should().Be(default);

        // Act
        await context.Bookings.AddAsync(booking);
        await context.SaveChangesAsync();

        var afterSave = DateTimeOffset.UtcNow;

        // Assert
        booking.CreatedAt.Should().NotBe(default);
        booking.CreatedAt.Should().BeOnOrAfter(beforeSave);
        booking.CreatedAt.Should().BeOnOrBefore(afterSave);

        var storedBooking = await context.Bookings.AsNoTracking().FirstAsync(x => x.Id == booking.Id);
        storedBooking.CreatedAt.Should().Be(booking.CreatedAt);
        storedBooking.CreatedAt.Should().BeOnOrAfter(beforeSave);
        storedBooking.CreatedAt.Should().BeOnOrBefore(afterSave);
    }

    /// <summary>
    /// Проверяет, что для одного события можно создать несколько броней с уникальными идентификаторами.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldCreateMultipleBookingsWithUniqueIds()
    {
        // Arrange
        var eventEntity = await SeedEventAsync(totalSeats: 3);
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;

        // Act
        var bookings = new[]
        {
            await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid()),
            await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid()),
            await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid())
        };

        // Assert
        bookings.Select(x => x.Id).Should().OnlyHaveUniqueItems();
        bookings.Should().OnlyContain(x => x.EventId == eventEntity.Id);
        bookings.Should().OnlyContain(x => x.Status == BookingStatus.Pending);

        using var verificationScope = CreateScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedEvent = await context.Events.AsNoTracking().FirstAsync(x => x.Id == eventEntity.Id);
        storedEvent.AvailableSeats.Should().Be(0);
    }

    /// <summary>
    /// Проверяет, что сервис возвращает бронирование по идентификатору.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenBookingExists()
    {
        // Arrange
        var eventEntity = await SeedEventAsync();
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;
        var created = await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());

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
        var firstEvent = await SeedEventAsync(title: "Первое событие");
        var secondEvent = await SeedEventAsync(title: "Второе событие");
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;

        var firstBooking = await service.CreateBookingAsync(firstEvent.Id, Guid.NewGuid());
        var secondBooking = await service.CreateBookingAsync(firstEvent.Id, Guid.NewGuid());
        await service.CreateBookingAsync(secondEvent.Id, Guid.NewGuid());

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
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;
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
        var eventEntity = await SeedEventAsync();
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;
        var created = await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());

        using (var scope = CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var trackedBooking = await context.Bookings.FirstAsync(x => x.Id == created.Id);
            trackedBooking.Confirm(DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }

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
        var eventEntity = await SeedEventAsync();
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;
        var created = await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());

        using (var scope = CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var trackedBooking = await context.Bookings.FirstAsync(x => x.Id == created.Id);
            trackedBooking.Reject(DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }

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
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;
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
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;
        var eventId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventId, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{eventId}*");
    }

    /// <summary>
    /// Проверяет, что сервис не создает бронь, если свободные места закончились.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenNoSeatsAvailable()
    {
        // Arrange
        var eventEntity = await SeedEventAsync(totalSeats: 1);
        using var serviceScope = CreateBookingServiceScope();
        var service = serviceScope.Service;

        await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NoAvailableSeatsException>();

        using var verificationScope = CreateScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedEvent = await context.Events.AsNoTracking().FirstAsync(x => x.Id == eventEntity.Id);
        storedEvent.AvailableSeats.Should().Be(0);
    }

    /// <summary>
    /// Проверяет, что конкурентные запросы не создают броней больше, чем доступно мест.
    /// </summary>
    [Theory]
    [InlineData(OverbookingTotalSeats, OverbookingRequestCount)]
    public async Task CreateBookingAsync_ShouldNotOverbook_WhenRequestsAreConcurrent(int totalSeats, int requestCount)
    {
        // Arrange
        var eventEntity = await SeedEventAsync(totalSeats: totalSeats);

        // Act
        var attempts = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                try
                {
                    await bookingService.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());
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

        using var verificationScope = CreateScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedEvent = await context.Events.AsNoTracking().FirstAsync(x => x.Id == eventEntity.Id);
        storedEvent.AvailableSeats.Should().Be(0);
    }

    /// <summary>
    /// Проверяет, что конкурентные успешные запросы создают брони с уникальными идентификаторами.
    /// </summary>
    [Theory]
    [InlineData(10, 10)]
    public async Task CreateBookingAsync_ShouldCreateUniqueIds_WhenConcurrentRequestsFitSeatsLimit(int totalSeats, int requestCount)
    {
        // Arrange
        var eventEntity = await SeedEventAsync(totalSeats: totalSeats);

        // Act
        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBookingService>();
                return await service.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());
            }));

        var bookings = await Task.WhenAll(tasks);

        // Assert
        bookings.Should().HaveCount(requestCount);
        bookings.Select(x => x.Id).Should().OnlyHaveUniqueItems();

        using var verificationScope = CreateScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedEvent = await context.Events.AsNoTracking().FirstAsync(x => x.Id == eventEntity.Id);
        storedEvent.AvailableSeats.Should().Be(totalSeats - requestCount);
    }

    /// <summary>
    /// Создает экземпляр сервиса для тестов.
    /// </summary>
    private ScopedService<IBookingService> CreateBookingServiceScope()
    {
        return CreateScopedService<IBookingService>();
    }

}
