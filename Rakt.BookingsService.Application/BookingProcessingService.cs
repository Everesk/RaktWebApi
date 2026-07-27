using Microsoft.Extensions.Logging;
using Rakt.BookingsService.Domain;

namespace Rakt.BookingsService.Application;

/// <summary>
/// Прикладной сценарий обработки ожидающих броней с повторными попытками.
/// </summary>
public sealed class BookingProcessingService(
    IBookingRepository bookings,
    BookingConfirmationService confirmationService,
    IBookingProcessingState processingState,
    ILogger<BookingProcessingService> logger) : IBookingProcessingService
{
    /// <summary>
    /// Обрабатывает ожидающие брони и отклоняет их после исчерпания лимита ошибок.
    /// </summary>
    /// <param name="attemptsLimit">Число ошибок, после которого бронь отклоняется.</param>
    /// <param name="cancellationToken">Токен отмены фоновой операции.</param>
    public async Task ProcessPendingAsync(int attemptsLimit, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attemptsLimit, 1);
        cancellationToken.ThrowIfCancellationRequested();

        var pendingBookingIds = await bookings.GetPendingIdsAsync(cancellationToken);

        foreach (var bookingId in pendingBookingIds)
        {
            await ProcessBookingAsync(bookingId, attemptsLimit, cancellationToken);
        }
    }

    /// <summary>
    /// Обрабатывает одну бронь и учитывает ошибки публикации подтверждения.
    /// </summary>
    private async Task ProcessBookingAsync(
        Guid bookingId,
        int attemptsLimit,
        CancellationToken cancellationToken)
    {
        if (!processingState.TryMarkProcessing(bookingId))
        {
            return;
        }

        try
        {
            var booking = await bookings.GetAsync(bookingId, cancellationToken);
            if (booking is null || booking.Status != BookingStatus.Pending)
            {
                processingState.ClearAttempts(bookingId);

                return;
            }

            try
            {
                await confirmationService.ConfirmAsync(bookingId, cancellationToken);
                processingState.ClearAttempts(bookingId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var attempt = processingState.RegisterAttempt(bookingId);
                logger.LogError(
                    exception,
                    "Не удалось обработать бронь {BookingId}. Номер неудачной попытки: {Attempt}.",
                    bookingId,
                    attempt);

                if (attempt >= attemptsLimit)
                {
                    booking.Reject(DateTimeOffset.UtcNow);
                    await bookings.SaveChangesAsync(cancellationToken);
                    processingState.ClearAttempts(bookingId);

                    return;
                }

                booking.ReturnToPending();
                await bookings.SaveChangesAsync(cancellationToken);
            }
        }
        finally
        {
            processingState.UnmarkProcessing(bookingId);
        }
    }
}
