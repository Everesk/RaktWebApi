using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rakt.BookingsService.Application;
using Rakt.Contracts.Messaging;

namespace Rakt.BookingsService.Infrastructure;

/// <summary>
/// Получает результаты резервирования мест и завершает обработку брони.
/// </summary>
public sealed class SeatsReservationResultConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<SeatsReservationResultConsumer> logger) : BackgroundService
{
    /// <summary>
    /// Подписывается на успешные и отклонённые результаты резервирования.
    /// </summary>
    /// <param name="stoppingToken">Токен остановки фонового сервиса.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuration = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 6000,
            HeartbeatIntervalMs = 2000
        };
        using var consumer = new ConsumerBuilder<string, string>(configuration).Build();
        consumer.Subscribe([BookingTopics.SeatsReserved, BookingTopics.SeatsReservationRejected]);
        logger.LogInformation("Подписчик результатов резервирования мест запущен");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    await ProcessMessageAsync(result.Topic, result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (ConsumeException exception)
                {
                    logger.LogWarning(
                        exception,
                        "Топики результатов резервирования ещё недоступны. Повторная попытка будет выполнена позже.");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Не удалось обработать результат резервирования. Сообщение будет прочитано повторно.");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Подписчик результатов резервирования мест остановлен");
        }
        finally
        {
            consumer.Close();
        }
    }

    /// <summary>
    /// Передаёт результат в прикладной сценарий в отдельной области зависимостей.
    /// </summary>
    private async Task ProcessMessageAsync(
        string topicName,
        string payload,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<BookingReservationResultService>();

        if (topicName == BookingTopics.SeatsReserved)
        {
            var message = JsonSerializer.Deserialize<SeatsReserved>(payload);
            if (message is null)
            {
                logger.LogWarning("Пропущен неверный результат успешного резервирования");

                return;
            }

            await service.HandleAsync(message, cancellationToken);

            return;
        }

        var rejectedMessage = JsonSerializer.Deserialize<SeatsReservationRejected>(payload);
        if (rejectedMessage is null)
        {
            logger.LogWarning("Пропущен неверный результат отказа в резервировании");

            return;
        }

        await service.HandleAsync(rejectedMessage, cancellationToken);
    }
}
