using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaktWebApi.Data;
using RaktWebApi.Models;
using RaktWebApi.Services;
using Rakt.Tests.Infrastructure;

namespace Rakt.Tests.Services;

/// <summary>
/// Тесты для обработчика бронирований.
/// </summary>
public class BookingProcessorTests : InMemoryDbTestBase
{
    /// <summary>
    /// Настраивает сервисы, необходимые для тестов <see cref="BookingProcessor"/>.
    /// </summary>
    /// <param name="services">Коллекция сервисов DI.</param>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddSingleton<IBookingProcessor, BookingProcessor>();
        services.AddLogging();
    }

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
    /// Возвращает singleton-обработчик бронирований.
    /// </summary>
    private IBookingProcessor GetProcessor()
    {
        return GetRequiredService<IBookingProcessor>();
    }
}
