using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaktApi.Infrastructure.Data.Interceptors;
using RaktApi.Infrastructure.Data;
using RaktApi.Domain;
using RaktApi.Application.Ports;
using RaktApi.Application.Services;
using RaktApi.Infrastructure.Repositories;
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
        services.AddScoped<TestCurrentUserContext>();
        services.AddScoped<ICurrentUserContext>(provider => provider.GetRequiredService<TestCurrentUserContext>());
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton<IBookingProcessor, BookingProcessor>();
        services.AddLogging();
    }

    /// <summary>
    /// Настраивает DbContext для тестов обработчика бронирований.
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
    /// Проверяет, что отклонение брони возвращает место в пул события.
    /// </summary>
    [Fact]
    public async Task TryRejectAsync_ShouldReleaseReservedSeat()
    {
        // Arrange
        var eventEntity = await SeedEventAsync(totalSeats: 1);
        using var bookingScope = CreateScope();
        var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();
        var booking = await bookingService.CreateBookingAsync(eventEntity.Id, Guid.NewGuid());

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
