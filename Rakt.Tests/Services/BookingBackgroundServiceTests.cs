using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;

namespace Rakt.Tests.Services;

/// <summary>
/// Тесты для фоновой обработки бронирований.
/// </summary>
public class BookingBackgroundServiceTests
{
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

        var processor = new RaktWebApi.Services.BookingProcessor(
            bookingRepository,
            NullLogger<RaktWebApi.Services.BookingProcessor>.Instance);

        var service = new RaktWebApi.Services.BookingBackgroundService(
            bookingRepository,
            processor,
            NullLogger<RaktWebApi.Services.BookingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(4));
        var processed = bookingRepository.GetById(booking.Id);

        // Assert
        processed.Should().NotBeNull();
        processed!.Status.Should().Be(BookingStatus.Confirmed);
        processed.ProcessedAt.Should().NotBeNull();

        cts.Cancel();
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
            startAt: new DateTime(2026, 4, 1, 10, 0, 0),
            endAt: new DateTime(2026, 4, 1, 11, 0, 0));
    }
}
