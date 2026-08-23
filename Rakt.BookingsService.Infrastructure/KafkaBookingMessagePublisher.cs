using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Rakt.BookingsService.Application;
using Rakt.Contracts.Messaging;

namespace Rakt.BookingsService.Infrastructure;

/// <summary>
/// Публикует все интеграционные сообщения броней единым Kafka-продюсером.
/// </summary>
public sealed class KafkaBookingMessagePublisher :
    IBookingMessagePublisher,
    IDisposable
{
    private readonly IProducer<string, string> _producer;

    /// <summary>
    /// Создаёт потокобезопасный продюсер на время жизни приложения.
    /// </summary>
    /// <param name="options">Настройки подключения к Kafka.</param>
    public KafkaBookingMessagePublisher(IOptions<KafkaOptions> options)
    {
        var configuration = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(configuration).Build();
    }

    /// <summary>
    /// Публикует запрос на создание брони.
    /// </summary>
    public Task PublishAsync(BookingRequested message, CancellationToken ct = default)
    {
        return PublishAsync(BookingTopics.Requested, message.EventId, message, ct);
    }

    /// <summary>
    /// Публикует запрос на отмену брони.
    /// </summary>
    public Task PublishAsync(BookingCancelled message, CancellationToken ct = default)
    {
        return PublishAsync(BookingTopics.Cancelled, message.EventId, message, ct);
    }

    /// <summary>
    /// Освобождает ресурсы Kafka-продюсера при остановке приложения.
    /// </summary>
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }

    /// <summary>
    /// Сериализует и публикует сообщение с ключом идентификатора события.
    /// </summary>
    private async Task PublishAsync(
        string topic,
        Guid eventId,
        object message,
        CancellationToken cancellationToken)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = eventId.ToString(),
            Value = JsonSerializer.Serialize(message)
        };

        await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
    }
}
