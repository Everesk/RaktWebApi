using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rakt.Contracts.Messaging;

namespace Rakt.EventsService.Infrastructure;

/// <summary>
/// Создаёт топик подтверждённых броней до запуска его потребителя.
/// </summary>
public sealed class KafkaTopicInitializer(
    IOptions<KafkaOptions> options,
    ILogger<KafkaTopicInitializer> logger) : IHostedService
{
    /// <summary>
    /// Создаёт топик, если он ещё отсутствует, не прерывая запуск при ошибке Kafka.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены запуска приложения.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        }).Build();

        try
        {
            await adminClient.CreateTopicsAsync(
                [new TopicSpecification
                {
                    Name = BookingTopics.Confirmed,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }]);

            logger.LogInformation("Создан Kafka-топик {TopicName}", BookingTopics.Confirmed);
        }
        catch (CreateTopicsException exception) when (IsTopicAlreadyExists(exception))
        {
            logger.LogDebug("Kafka-топик {TopicName} уже существует", BookingTopics.Confirmed);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Не удалось создать Kafka-топик {TopicName}. Подписчик продолжит запуск.",
                BookingTopics.Confirmed);
        }
    }

    /// <summary>
    /// Не требует действий при штатной остановке приложения.
    /// </summary>
    /// <param name="cancellationToken">Токен остановки приложения.</param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Проверяет, что ошибка создания вызвана существующим топиком.
    /// </summary>
    private static bool IsTopicAlreadyExists(CreateTopicsException exception)
    {
        return exception.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists);
    }
}
