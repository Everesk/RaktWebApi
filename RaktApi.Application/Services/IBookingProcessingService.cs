namespace RaktApi.Application.Services;

/// <summary>
/// Интерфейс use case для обработки ожидающих бронирований.
/// </summary>
public interface IBookingProcessingService
{
    /// <summary>
    /// Обрабатывает ожидающие бронирования и отклоняет их после исчерпания допустимых попыток.
    /// </summary>
    /// <param name="attemptsLimit">Максимальное количество неуспешных попыток обработки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task ProcessPendingAsync(int attemptsLimit, CancellationToken cancellationToken = default);
}
