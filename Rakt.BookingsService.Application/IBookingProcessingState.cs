namespace Rakt.BookingsService.Application;

/// <summary>
/// Хранит в памяти процесса состояние обработки и число ошибок брони.
/// </summary>
public interface IBookingProcessingState
{
    /// <summary>
    /// Пытается пометить бронь как выполняемую.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    /// <returns><see langword="true"/>, если обработка ещё не выполняется.</returns>
    bool TryMarkProcessing(Guid bookingId);

    /// <summary>
    /// Снимает отметку о выполняемой обработке.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    void UnmarkProcessing(Guid bookingId);

    /// <summary>
    /// Увеличивает и возвращает число неудачных попыток обработки.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    /// <returns>Число ошибок после увеличения счётчика.</returns>
    int RegisterAttempt(Guid bookingId);

    /// <summary>
    /// Удаляет сохранённый счётчик ошибок.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    void ClearAttempts(Guid bookingId);
}
