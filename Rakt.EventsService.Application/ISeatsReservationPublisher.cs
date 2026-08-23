using Rakt.Contracts.Messaging;

namespace Rakt.EventsService.Application;

/// <summary>
/// Публикует результаты резервирования мест для сервиса броней.
/// </summary>
public interface ISeatsReservationPublisher
{
    /// <summary>
    /// Публикует успешное резервирование места.
    /// </summary>
    /// <param name="message">Сообщение об успешном резервировании.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task PublishAsync(SeatsReserved message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Публикует отказ в резервировании места.
    /// </summary>
    /// <param name="message">Сообщение об отказе.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task PublishAsync(SeatsReservationRejected message, CancellationToken cancellationToken = default);
}
