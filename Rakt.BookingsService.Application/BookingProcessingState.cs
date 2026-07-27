namespace Rakt.BookingsService.Application;

/// <summary>
/// Потокобезопасное состояние обработки броней в одном экземпляре приложения.
/// </summary>
public sealed class BookingProcessingState : IBookingProcessingState
{
    private readonly HashSet<Guid> _processingBookings = [];
    private readonly Dictionary<Guid, int> _processingAttempts = [];
    private readonly object _syncRoot = new();

    /// <summary>
    /// Пытается пометить бронь как выполняемую.
    /// </summary>
    public bool TryMarkProcessing(Guid bookingId)
    {
        lock (_syncRoot)
        {
            return _processingBookings.Add(bookingId);
        }
    }

    /// <summary>
    /// Снимает отметку о выполняемой обработке.
    /// </summary>
    public void UnmarkProcessing(Guid bookingId)
    {
        lock (_syncRoot)
        {
            _processingBookings.Remove(bookingId);
        }
    }

    /// <summary>
    /// Увеличивает число ошибок обработки.
    /// </summary>
    public int RegisterAttempt(Guid bookingId)
    {
        lock (_syncRoot)
        {
            _processingAttempts.TryGetValue(bookingId, out var currentAttempt);
            var nextAttempt = currentAttempt + 1;
            _processingAttempts[bookingId] = nextAttempt;

            return nextAttempt;
        }
    }

    /// <summary>
    /// Удаляет число ошибок обработки.
    /// </summary>
    public void ClearAttempts(Guid bookingId)
    {
        lock (_syncRoot)
        {
            _processingAttempts.Remove(bookingId);
        }
    }
}
