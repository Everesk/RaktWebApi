using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;
using RaktWebApi.Services;

namespace Rakt.Tests.Services;

/// <summary>
/// Тесты для обработчика бронирований.
/// </summary>
public class BookingProcessorTests
{
    /// <summary>
    /// Проверяет, что отклонение брони возвращает место в пул события.
    /// </summary>
    [Fact]
    public async Task TryRejectAsync_ShouldReleaseReservedSeat()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateEvent(totalSeats: 1);
        eventEntity.TryReserveSeats().Should().BeTrue();
        eventRepository.Add(eventEntity);

        var bookingRepository = new InMemoryBookingRepository();
        var booking = new Booking(eventEntity.Id);
        bookingRepository.Add(booking);

        var processor = new BookingProcessor(
            bookingRepository,
            eventRepository,
            NullLogger<BookingProcessor>.Instance);

        // Act
        var rejected = await processor.TryRejectAsync(booking);

        // Assert
        rejected.Should().BeTrue();
        bookingRepository.GetById(booking.Id)!.Status.Should().Be(BookingStatus.Rejected);
        eventRepository.GetById(eventEntity.Id)!.AvailableSeats.Should().Be(1);
    }

    private static Event CreateEvent(int totalSeats)
    {
        return new Event(
            title: "Тестовое событие",
            description: null,
            startAt: new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
            endAt: new DateTimeOffset(2026, 4, 1, 11, 0, 0, TimeSpan.Zero),
            totalSeats: totalSeats);
    }
}
