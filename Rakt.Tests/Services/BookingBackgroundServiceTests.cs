using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaktWebApi.Data.Repositories;
using RaktWebApi.Options;
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
        services.AddOptions<BookingProcessingOptions>().Configure(options => options.AttemptsLimit = 3);
        await using var provider = services.BuildServiceProvider();

        var service = new BookingBackgroundService(
            bookingRepository,
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<BookingProcessingOptions>>(),
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
    /// Проверяет, что неожиданная ошибка переводит бронь в Rejected.
    /// </summary>
    [Fact]
    public async Task BackgroundService_ShouldRejectBooking_WhenProcessorThrows()
    {
        // Arrange
        var eventRepository = new InMemoryEventRepository();
        var eventEntity = CreateEvent();
        eventRepository.Add(eventEntity);

        var bookingRepository = new InMemoryBookingRepository();
        var booking = new Booking(eventEntity.Id);
        bookingRepository.Add(booking);
        var processor = new CountingThrowingBookingProcessor(bookingRepository);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IBookingRepository>(bookingRepository);
        services.AddSingleton<IBookingProcessor>(processor);
        services.AddOptions<BookingProcessingOptions>().Configure(options => options.AttemptsLimit = 2);
        await using var provider = services.BuildServiceProvider();

        var service = new BookingBackgroundService(
            bookingRepository,
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<BookingProcessingOptions>>(),
            NullLogger<BookingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);

        Booking? processed = null;
        var completed = SpinWait.SpinUntil(() =>
        {
            processed = bookingRepository.GetById(booking.Id);
            return processed?.Status == BookingStatus.Rejected;
        }, TimeSpan.FromSeconds(30));

        // Assert
        completed.Should().BeTrue("неожиданная ошибка должна переводить бронь в Rejected");
        processed.Should().NotBeNull();
        processed!.Status.Should().Be(BookingStatus.Rejected);
        processed.ProcessedAt.Should().NotBeNull();
        processor.ProcessCalls.Should().Be(2);
        processor.RejectCalls.Should().Be(1);

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

    private sealed class CountingThrowingBookingProcessor(IBookingRepository bookingRepository) : IBookingProcessor
    {
        public int ProcessCalls { get; private set; }

        public int RejectCalls { get; private set; }

        public Task ProcessAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            ProcessCalls++;
            throw new InvalidOperationException("Ошибка обработки бронирования");
        }

        public Task<bool> TryRejectAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            RejectCalls++;
            booking.Reject(DateTimeOffset.UtcNow);
            bookingRepository.Update(booking);
            return Task.FromResult(true);
        }
    }
}
