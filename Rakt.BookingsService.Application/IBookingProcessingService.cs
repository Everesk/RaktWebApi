namespace Rakt.BookingsService.Application;

/// <summary>
/// Обрабатывает брони, ожидающие подтверждения.
/// </summary>
public interface IBookingProcessingService
{
    /// <summary>
    /// Обрабатывает все брони в статусе ожидания.
    /// </summary>
    /// <param name="attemptsLimit">Число ошибок, после которого бронь отклоняется.</param>
    /// <param name="cancellationToken">Токен отмены фоновой операции.</param>
    Task ProcessPendingAsync(int attemptsLimit, CancellationToken cancellationToken = default);
}
