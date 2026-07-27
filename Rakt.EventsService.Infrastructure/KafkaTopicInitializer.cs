using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rakt.Contracts.Messaging;

namespace Rakt.EventsService.Infrastructure;

/// <summary>
/// Создаёт топики, потребляемые сервисом событий, до запуска подписчиков.
/// </summary>
public sealed class KafkaTopicInitializer(
    IOptions<KafkaOptions> options,
    ILogger<KafkaTopicInitializer> logger) : IHostedService
{
    /// <summary>
    /// Создаёт отсутствующие топики, не прерывая запуск при ошибке Kafka.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены запуска приложения.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        }).Build();

        foreach (var topicName in GetTopicNames())
        {
            await CreateTopicAsync(adminClient, topicName);
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
    /// Возвращает имена топиков, на которые подписан сервис событий.
    /// </summary>
    private static IReadOnlyCollection<string> GetTopicNames()
    {
        return
        [
            BookingTopics.Requested,
            BookingTopics.Cancelled,
            BookingTopics.SeatsReserved,
            BookingTopics.SeatsReservationRejected
        ];
    }

    /// <summary>
    /// Создаёт один Kafka-топик или фиксирует, что он уже существует.
    /// </summary>
    private async Task CreateTopicAsync(IAdminClient adminClient, string topicName)
    {
        try
        {
            await adminClient.CreateTopicsAsync(
                [new TopicSpecification
                {
                    Name = topicName,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }]);

            logger.LogInformation("Создан Kafka-топик {TopicName}", topicName);
        }
        catch (CreateTopicsException exception) when (IsTopicAlreadyExists(exception))
        {
            logger.LogDebug("Kafka-топик {TopicName} уже существует", topicName);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Не удалось создать Kafka-топик {TopicName}. Подписчик продолжит запуск.",
                topicName);
        }
    }

    /// <summary>
    /// Проверяет, что ошибка создания вызвана существующим топиком.
    /// </summary>
    private static bool IsTopicAlreadyExists(CreateTopicsException exception)
    {
        return exception.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists);
    }
}
