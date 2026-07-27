using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RaktApi.Infrastructure.Data.Interceptors;
using RaktApi.Infrastructure.Data;
using RaktApi.Infrastructure.BackgroundServices;
using RaktApi.Infrastructure.Options;
using RaktApi.Domain;
using RaktApi.Application.Ports;
using RaktApi.Application.Services;
using RaktApi.Infrastructure.Repositories;
using Rakt.Tests.Infrastructure;

namespace Rakt.Tests.Services;

/// <summary>
/// Тесты для фоновой обработки бронирований.
/// </summary>
[Trait("Category", "Unit")]
public class BookingBackgroundServiceTests : InMemoryDbTestBase
{
    /// <summary>
    /// Настраивает сервисы, необходимые для тестов <see cref="BookingBackgroundService"/>.
    /// </summary>
    /// <param name="services">Коллекция сервисов DI.</param>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<TestCurrentUserContext>();
        services.AddScoped<ICurrentUserContext>(provider => provider.GetRequiredService<TestCurrentUserContext>());
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingProcessor, BookingProcessor>();
        services.AddSingleton<IBookingProcessingState, BookingProcessingState>();
        services.AddScoped<IBookingProcessingService, BookingProcessingService>();
        services.AddOptions<BookingProcessingOptions>().Configure(options => options.AttemptsLimit = 3);
        services.AddLogging();
    }

    /// <summary>
    /// Настраивает DbContext для тестов фоновой обработки бронирований.
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
    /// Проверяет, что счётчик неудачных попыток сохраняется между scope фонового обработчика.
    /// </summary>
    [Fact]
    public void ProcessingState_ShouldPersistAttemptsBetweenScopes()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        using var firstScope = CreateScope();
        using var secondScope = CreateScope();
        var firstState = firstScope.ServiceProvider.GetRequiredService<IBookingProcessingState>();
        var secondState = secondScope.ServiceProvider.GetRequiredService<IBookingProcessingState>();

        // Act
        var firstAttempt = firstState.RegisterAttempt(bookingId);
        var secondAttempt = secondState.RegisterAttempt(bookingId);

        // Assert
        firstState.Should().BeSameAs(secondState);
        firstAttempt.Should().Be(1);
        secondAttempt.Should().Be(2);
    }

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
    /// Создает Pending-бронь для теста.
    /// </summary>
    private async Task<Booking> CreatePendingBookingAsync(Guid eventId)
    {
        using var scope = CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        return await bookingService.CreateBookingAsync(eventId, Guid.NewGuid());
    }

    /// <summary>
    /// Возвращает фабрику scopes для тестового контейнера.
    /// </summary>
    private IServiceScopeFactory CreateScopeFactory()
    {
        return ServiceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    /// <summary>
    /// Создает конфигурированный фоновой сервис.
    /// </summary>
    private BookingBackgroundService CreateBackgroundService()
    {
        return new BookingBackgroundService(
            CreateScopeFactory(),
            Options.Create(new BookingProcessingOptions { AttemptsLimit = 3 }),
            NullLogger<BookingBackgroundService>.Instance);
    }
}
