using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaktWebApi.Data;
using RaktWebApi.Models;
using RaktWebApi.Services;

namespace Rakt.Tests.Services;

/// <summary>
/// Тесты для обработчика бронирований.
/// </summary>
public class BookingProcessorTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Проверяет, что отклонение брони возвращает место в пул события.
    /// </summary>
    [Fact]
    public async Task TryRejectAsync_ShouldReleaseReservedSeat()
    {
        // Arrange
        var eventEntity = await SeedEventAsync(totalSeats: 1);
        using var bookingScope = CreateScope();
        var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();
        var booking = await bookingService.CreateBookingAsync(eventEntity.Id);

        var processor = GetProcessor();

        // Act
        var rejected = await processor.TryRejectAsync(booking);

        // Assert
        rejected.Should().BeTrue();

        using var verificationScope = CreateScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedBooking = await context.Bookings.AsNoTracking().FirstAsync(x => x.Id == booking.Id);
        var storedEvent = await context.Events.AsNoTracking().FirstAsync(x => x.Id == eventEntity.Id);

        storedBooking.Status.Should().Be(BookingStatus.Rejected);
        storedEvent.AvailableSeats.Should().Be(1);
    }

    /// <summary>
    /// Создает событие для теста.
    /// </summary>
    private async Task<Event> SeedEventAsync(int totalSeats)
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventEntity = new Event(
            title: "Тестовое событие",
            description: null,
            startAt: new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
            endAt: new DateTimeOffset(2026, 4, 1, 11, 0, 0, TimeSpan.Zero),
            totalSeats: totalSeats);

        await context.Events.AddAsync(eventEntity);
        await context.SaveChangesAsync();
        return eventEntity;
    }

    /// <summary>
    /// Возвращает общий scope для теста.
    /// </summary>
    private IServiceScope CreateScope()
    {
        EnsureProvider();
        return _serviceProvider!.CreateScope();
    }

    /// <summary>
    /// Возвращает singleton-обработчик бронирований.
    /// </summary>
    private IBookingProcessor GetProcessor()
    {
        EnsureProvider();
        using var scope = CreateScope();
        return scope.ServiceProvider.GetRequiredService<IBookingProcessor>();
    }

    /// <summary>
    /// Подготавливает контейнер DI для текущего теста.
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
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Освобождает ресурсы тестового контейнера.
    /// </summary>
    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
