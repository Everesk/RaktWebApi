using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;
using RaktWebApi.Services;

namespace Rakt.Tests.Services;

/// <summary>
/// Тесты для фоновой обработки бронирований.
/// </summary>
public class BookingBackgroundServiceTests
{
    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute, int second)
    {
        return new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
    }

    /// <summary>
    /// Проверяет, что фоновый сервис переводит Pending-бронь в Confirmed.
    /// </summary>
    [Fact]
    public async Task BackgroundService_ShouldConfirmPendingBooking()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateEvent();
        eventRepository.Add(eventEntity);

        var bookingRepository = new InMemoryBookingRepository();
        var booking = new Booking(eventEntity.Id);
        bookingRepository.Add(booking);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IBookingRepository>(bookingRepository);
        services.AddScoped<IBookingProcessor, BookingProcessor>();
        await using var provider = services.BuildServiceProvider();

        var service = new BookingBackgroundService(
            bookingRepository,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        // Act
        await service.StartAsync(cts.Token);

        Booking? processed = null;
        var completed = SpinWait.SpinUntil(() =>
        {
            processed = bookingRepository.GetById(booking.Id);
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
    /// Создает событие для теста.
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
