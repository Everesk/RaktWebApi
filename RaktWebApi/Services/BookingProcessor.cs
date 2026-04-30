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
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Начата обработка брони {BookingId}", booking.Id);

        await Task.Delay(ExternalSystemDelay, cancellationToken);

        booking.Confirm(DateTimeOffset.UtcNow);
        bookingRepository.Update(booking);

        logger.LogInformation(
            "Бронь {BookingId} переведена в статус {Status}",
            booking.Id,
            booking.Status);
    }

    /// <summary>
    /// Отклоняет бронирование и сохраняет состояние. Безопасен для вызова, может бросить только OperationCanceledException при отмене через CancellationToken.
    /// </summary>
    public Task<bool> TryRejectAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            ArgumentNullException.ThrowIfNull(booking);
            logger.LogWarning("Отклонение брони {BookingId}", booking.Id);

            booking.Reject(DateTimeOffset.UtcNow);
            bookingRepository.Update(booking);

            logger.LogWarning(
                "Бронь {BookingId} переведена в статус {Status}",
                booking.Id,
                booking.Status);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось отклонить бронь {BookingId}", booking.Id);
            return Task.FromResult(false);
        }
    }
}
