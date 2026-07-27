using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Rakt.BookingsService.Application;
using Rakt.Contracts.Messaging;

namespace Rakt.BookingsService.Infrastructure;

/// <summary>Публикует подтверждённые брони в Kafka.</summary>
public sealed class KafkaBookingConfirmedPublisher : IBookingConfirmedPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    /// <summary>Создаёт потокобезопасный продюсер Kafka один раз на время жизни приложения.</summary>
    public KafkaBookingConfirmedPublisher(IOptions<KafkaOptions> options)
    {
        var configuration = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(configuration).Build();
    }

    /// <summary>Сериализует событие в JSON и отправляет его в топик подтверждённых броней.</summary>
    public async Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message);
        var kafkaMessage = new Message<string, string>
        {
            Key = message.EventId.ToString(),
            Value = payload
        };

        await _producer.ProduceAsync(
            BookingTopics.Confirmed,
            kafkaMessage,
            cancellationToken);
    }

    /// <summary>Освобождает ресурсы продюсера при остановке приложения.</summary>
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
