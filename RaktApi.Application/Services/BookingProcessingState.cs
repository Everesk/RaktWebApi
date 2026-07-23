namespace RaktApi.Application.Services;

/// <summary>
/// Потокобезопасное состояние обработки бронирований в рамках одного экземпляра приложения.
/// </summary>
public sealed class BookingProcessingState : IBookingProcessingState
{
    private readonly HashSet<Guid> processingBookings = [];
    private readonly Dictionary<Guid, int> processingAttempts = [];
    private readonly object syncRoot = new();

    /// <inheritdoc />
    public bool TryMarkProcessing(Guid bookingId)
    {
        lock (syncRoot)
        {
            return processingBookings.Add(bookingId);
        }
    }

    /// <inheritdoc />
    public void UnmarkProcessing(Guid bookingId)
    {
        lock (syncRoot)
        {
            processingBookings.Remove(bookingId);
        }
    }

    /// <inheritdoc />
    public int RegisterAttempt(Guid bookingId)
    {
        lock (syncRoot)
        {
            processingAttempts.TryGetValue(bookingId, out var currentAttempt);
            var nextAttempt = currentAttempt + 1;
            processingAttempts[bookingId] = nextAttempt;
            return nextAttempt;
        }
    }

    /// <inheritdoc />
    public void ClearAttempts(Guid bookingId)
    {
        lock (syncRoot)
        {
            processingAttempts.Remove(bookingId);
        }
    }
}
