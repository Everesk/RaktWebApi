using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rakt.Contracts.Messaging;
using Rakt.EventsService.Application;

namespace Rakt.EventsService.Infrastructure;

/// <summary>
/// Получает отменённые брони из Kafka и возвращает места событиям.
/// </summary>
public sealed class BookingCancelledConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<BookingCancelledConsumer> logger) : BackgroundService
{
    /// <summary>
    /// Подписывается на топик отменённых броней и обрабатывает сообщения.
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
        consumer.Subscribe(BookingTopics.Cancelled);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> result;

                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException exception)
                {
                    logger.LogError(exception, "Не удалось прочитать отмену брони из Kafka");

                    continue;
                }

                try
                {
                    await ProcessMessageAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Не удалось обработать отмену брони из Kafka");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Подписчик Kafka отменённых броней остановлен");
        }
        finally
        {
            consumer.Close();
        }
    }

    /// <summary>
    /// Десериализует отмену и возвращает место события в отдельной области зависимостей.
    /// </summary>
    private async Task ProcessMessageAsync(string payload, CancellationToken cancellationToken)
    {
        var bookingCancelled = JsonSerializer.Deserialize<BookingCancelled>(payload);
        if (bookingCancelled is null)
        {
            logger.LogWarning("Пропущено Kafka-сообщение с пустым или неверным телом отмены");

            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventEntity = await repository.GetForUpdateAsync(bookingCancelled.EventId, cancellationToken);
        if (eventEntity is null)
        {
            logger.LogWarning(
                "Пропущена отмена брони {BookingId}: событие {EventId} не найдено",
                bookingCancelled.BookingId,
                bookingCancelled.EventId);

            return;
        }

        eventEntity.ReleaseSeat();
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Для события {EventId} возвращено место по отмене брони {BookingId}",
            bookingCancelled.EventId,
            bookingCancelled.BookingId);
    }
}
