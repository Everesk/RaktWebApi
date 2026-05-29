using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RaktWebApi.Data;
using RaktWebApi.Models;
using RaktWebApi.Options;
using RaktWebApi.Services;

namespace Rakt.Tests.Services;

/// <summary>
/// Тесты для фоновой обработки бронирований.
/// </summary>
public class BookingBackgroundServiceTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Проверяет, что фоновый сервис переводит Pending-бронь в Confirmed.
    /// </summary>
    [Fact]
    public async Task BackgroundService_ShouldConfirmPendingBooking()
    {
        // Arrange
        var eventEntity = await SeedEventAsync();
        var booking = await CreatePendingBookingAsync(eventEntity.Id);
        var service = CreateBackgroundService();
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);

        Booking? processed = null;
        var completed = SpinWait.SpinUntil(() =>
        {
            using var scope = CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            processed = context.Bookings.AsNoTracking().FirstOrDefault(x => x.Id == booking.Id);
            return processed?.Status == BookingStatus.Confirmed;
        }, TimeSpan.FromSeconds(30));

        // Assert
        completed.Should().BeTrue("фоновая обработка должна завершиться в течение 30 секунд");
        processed.Should().NotBeNull();
        processed!.Status.Should().Be(BookingStatus.Confirmed);
        processed.ProcessedAt.Should().NotBeNull();

        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Проверяет, что бронь отклоняется, если событие удалено до фоновой обработки.
    /// </summary>
    [Fact]
    public async Task BackgroundService_ShouldRejectBooking_WhenEventWasDeletedBeforeProcessing()
    {
        // Arrange
        var eventEntity = await SeedEventAsync();
        var booking = await CreatePendingBookingAsync(eventEntity.Id);

        using (var scope = CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storedEvent = await context.Events.FirstAsync(x => x.Id == eventEntity.Id);
            context.Events.Remove(storedEvent);
            await context.SaveChangesAsync();
        }

        var service = CreateBackgroundService();
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);

        Booking? processed = null;
        var completed = SpinWait.SpinUntil(() =>
        {
            using var scope = CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            processed = context.Bookings.AsNoTracking().FirstOrDefault(x => x.Id == booking.Id);
            return processed?.Status == BookingStatus.Rejected;
        }, TimeSpan.FromSeconds(30));

        // Assert
        completed.Should().BeTrue("бронь должна быть отклонена, если событие удалено");
        processed.Should().NotBeNull();
        processed!.Status.Should().Be(BookingStatus.Rejected);
        processed.ProcessedAt.Should().NotBeNull();

        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Создает событие для теста.
    /// </summary>
    private async Task<Event> SeedEventAsync()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventEntity = CreateEvent();

        await context.Events.AddAsync(eventEntity);
        await context.SaveChangesAsync();
        return eventEntity;
    }

    /// <summary>
    /// Создает Pending-бронь для теста.
    /// </summary>
    private async Task<Booking> CreatePendingBookingAsync(Guid eventId)
    {
        using var scope = CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        return await bookingService.CreateBookingAsync(eventId);
    }

    /// <summary>
    /// Возвращает общий scope для работы с тестовой базой.
    /// </summary>
    private IServiceScope CreateScope()
    {
        EnsureProvider();
        return _serviceProvider!.CreateScope();
    }

    /// <summary>
    /// Возвращает фабрику scopes для тестового контейнера.
    /// </summary>
    private IServiceScopeFactory CreateScopeFactory()
    {
        EnsureProvider();
        return _serviceProvider!.GetRequiredService<IServiceScopeFactory>();
    }

    /// <summary>
    /// Создает конфигурированный фоновой сервис.
    /// </summary>
    private BookingBackgroundService CreateBackgroundService()
    {
        EnsureProvider();
        return new BookingBackgroundService(
            CreateScopeFactory(),
            Options.Create(new BookingProcessingOptions { AttemptsLimit = 3 }),
            NullLogger<BookingBackgroundService>.Instance);
    }

    /// <summary>
    /// Подготавливает DI-контейнер для текущего теста.
    /// </summary>
    private void EnsureProvider()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));
        services.AddScoped<IBookingService, BookingService>();
        services.AddSingleton<IBookingProcessor, BookingProcessor>();
        services.AddOptions<BookingProcessingOptions>().Configure(options => options.AttemptsLimit = 3);
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Создает событие для теста.
    /// </summary>
    private static Event CreateEvent()
    {
        return new Event(
            title: "Тестовое событие",
            description: null,
            startAt: new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
            endAt: new DateTimeOffset(2026, 4, 1, 11, 0, 0, TimeSpan.Zero),
            totalSeats: 10);
    }

    /// <summary>
    /// Освобождает ресурсы тестового контейнера.
    /// </summary>
    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

}
