using Microsoft.Extensions.Logging;
using RaktApi.Application.Ports;
using RaktApi.Domain;

namespace RaktApi.Application.Services;

/// <summary>
/// Use case для обработки бронирований, ожидающих подтверждения.
/// </summary>
public sealed class BookingProcessingService(
    IBookingRepository bookingRepository,
    IBookingProcessor bookingProcessor,
    IBookingProcessingState processingState,
    ILogger<BookingProcessingService> logger) : IBookingProcessingService
{
    /// <inheritdoc />
    public async Task ProcessPendingAsync(int attemptsLimit, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attemptsLimit, 1);
        cancellationToken.ThrowIfCancellationRequested();

        var pendingBookingIds = await bookingRepository.GetPendingIdsAsync(cancellationToken);
        var tasks = pendingBookingIds.Select(bookingId => ProcessBookingAsync(bookingId, attemptsLimit, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Обрабатывает одну ожидающую бронь и учитывает число неудачных попыток.
    /// </summary>
    private async Task ProcessBookingAsync(Guid bookingId, int attemptsLimit, CancellationToken cancellationToken)
    {
        if (!processingState.TryMarkProcessing(bookingId))
        {
            return;
        }

        try
        {
            var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            if (booking is null || booking.Status != BookingStatus.Pending)
            {
                processingState.ClearAttempts(bookingId);
                return;
            }

            try
            {
                await bookingProcessor.ProcessAsync(booking, cancellationToken);
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

                if (attempt >= attemptsLimit && await bookingProcessor.TryRejectAsync(booking, cancellationToken))
                {
                    processingState.ClearAttempts(bookingId);
                }
            }
        }
        finally
        {
            processingState.UnmarkProcessing(bookingId);
        }
    }
}
