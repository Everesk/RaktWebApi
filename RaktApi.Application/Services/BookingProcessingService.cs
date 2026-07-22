using RaktApi.Application.Ports;
using RaktApi.Domain;

namespace RaktApi.Application.Services;

/// <summary>
/// Use case для обработки бронирований, ожидающих подтверждения.
/// </summary>
public sealed class BookingProcessingService(
    IBookingRepository bookingRepository,
    IBookingProcessor bookingProcessor) : IBookingProcessingService
{
    private readonly HashSet<Guid> processingBookings = [];
    private readonly Dictionary<Guid, int> processingAttempts = [];
    private readonly object syncRoot = new();

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
        if (!TryMarkProcessing(bookingId))
        {
            return;
        }

        try
        {
            var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            if (booking is null || booking.Status != BookingStatus.Pending)
            {
                ClearAttempts(bookingId);
                return;
            }

            try
            {
                await bookingProcessor.ProcessAsync(booking, cancellationToken);
                ClearAttempts(bookingId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                var attempt = RegisterAttempt(bookingId);
                if (attempt >= attemptsLimit && await bookingProcessor.TryRejectAsync(booking, cancellationToken))
                {
                    ClearAttempts(bookingId);
                }
            }
        }
        finally
        {
            UnmarkProcessing(bookingId);
        }
    }

    /// <summary>
    /// Помечает бронь как выполняемую, если она ещё не обрабатывается.
    /// </summary>
    private bool TryMarkProcessing(Guid bookingId)
    {
        lock (syncRoot)
        {
            return processingBookings.Add(bookingId);
        }
    }

    /// <summary>
    /// Снимает отметку о выполняемой обработке брони.
    /// </summary>
    private void UnmarkProcessing(Guid bookingId)
    {
        lock (syncRoot)
        {
            processingBookings.Remove(bookingId);
        }
    }

    /// <summary>
    /// Увеличивает счётчик неудачных попыток обработки брони.
    /// </summary>
    private int RegisterAttempt(Guid bookingId)
    {
        lock (syncRoot)
        {
            processingAttempts.TryGetValue(bookingId, out var currentAttempt);
            var nextAttempt = currentAttempt + 1;
            processingAttempts[bookingId] = nextAttempt;
            return nextAttempt;
        }
    }

    /// <summary>
    /// Очищает счётчик попыток обработки брони.
    /// </summary>
    private void ClearAttempts(Guid bookingId)
    {
        lock (syncRoot)
        {
            processingAttempts.Remove(bookingId);
        }
    }
}
