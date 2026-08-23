using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Rakt.Contracts.Messaging;
using Rakt.EventsService.Application;

namespace Rakt.EventsService.Infrastructure;

/// <summary>
/// Публикует результаты резервирования мест в Kafka.
/// </summary>
public sealed class KafkaSeatsReservationPublisher : ISeatsReservationPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    /// <summary>
    /// Создаёт потокобезопасный продюсер на время жизни приложения.
    /// </summary>
    /// <param name="options">Настройки подключения к Kafka.</param>
    public KafkaSeatsReservationPublisher(IOptions<KafkaOptions> options)
    {
        var configuration = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(configuration).Build();
    }

    /// <inheritdoc />
    public Task PublishAsync(SeatsReserved message, CancellationToken cancellationToken = default)
    {
        return PublishAsync(BookingTopics.SeatsReserved, message.EventId, message, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(SeatsReservationRejected message, CancellationToken cancellationToken = default)
    {
        return PublishAsync(BookingTopics.SeatsReservationRejected, message.EventId, message, cancellationToken);
    }

    /// <summary>
    /// Отправляет JSON-сообщение с ключом идентификатора события.
    /// </summary>
    private async Task PublishAsync(
        string topicName,
        Guid eventId,
        object message,
        CancellationToken cancellationToken)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = eventId.ToString(),
            Value = JsonSerializer.Serialize(message)
        };

        await _producer.ProduceAsync(topicName, kafkaMessage, cancellationToken);
    }

    /// <summary>
    /// Освобождает ресурсы Kafka-продюсера.
    /// </summary>
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
