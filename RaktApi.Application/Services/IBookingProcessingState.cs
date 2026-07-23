namespace RaktApi.Application.Services;

/// <summary>
/// Хранит в памяти процесса состояние выполнения и число неудачных попыток обработки бронирований.
/// </summary>
public interface IBookingProcessingState
{
    /// <summary>
    /// Пытается пометить бронь как выполняемую.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    /// <returns><see langword="true"/>, если бронь не обрабатывалась и успешно помечена; иначе <see langword="false"/>.</returns>
    bool TryMarkProcessing(Guid bookingId);

    /// <summary>
    /// Снимает отметку о выполняемой обработке брони.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    void UnmarkProcessing(Guid bookingId);

    /// <summary>
    /// Увеличивает и возвращает количество неудачных попыток обработки брони.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    /// <returns>Количество неудачных попыток после увеличения счётчика.</returns>
    int RegisterAttempt(Guid bookingId);

    /// <summary>
    /// Удаляет счётчик неудачных попыток обработки брони.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    void ClearAttempts(Guid bookingId);
}
