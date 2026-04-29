using RaktWebApi.Data.Repositories;
using RaktWebApi.Models;

namespace RaktWebApi.Services;

/// <summary>
/// Обработчик бронирований.
/// </summary>
public sealed class BookingProcessor(
    IBookingRepository bookingRepository,
    ILogger<BookingProcessor> logger) : IBookingProcessor
{
    private static readonly TimeSpan ExternalSystemDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Обрабатывает бронирование в фоне.
    /// </summary>
    public async Task ProcessAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);

        logger.LogInformation("Начата обработка брони {BookingId}", booking.Id);

        await Task.Delay(ExternalSystemDelay, cancellationToken);

        booking.Confirm(DateTime.Now);
        bookingRepository.Update(booking);

        logger.LogInformation(
            "Бронь {BookingId} переведена в статус {Status}",
            booking.Id,
            booking.Status);
    }
}
