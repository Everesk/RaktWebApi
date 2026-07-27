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
/// Получает подтверждённые брони из Kafka и уменьшает остаток мест события.
/// </summary>
public sealed class BookingConfirmedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<BookingConfirmedConsumer> logger) : BackgroundService
{
    /// <summary>
    /// Подписывается на Kafka-топик и последовательно обрабатывает сообщения.
    /// </summary>
    /// <param name="stoppingToken">Токен остановки фонового сервиса.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfiguration = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        using var consumer = new ConsumerBuilder<string, string>(consumerConfiguration).Build();
        consumer.Subscribe(BookingTopics.Confirmed);
        logger.LogInformation(
            "Подписчик Kafka запущен для топика {TopicName} и группы {ConsumerGroup}",
            BookingTopics.Confirmed,
            options.Value.ConsumerGroup);

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
                    logger.LogError(exception, "Не удалось прочитать сообщение из Kafka");

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
                    logger.LogError(
                        exception,
                        "Не удалось обработать Kafka-сообщение из топика {TopicName}",
                        BookingTopics.Confirmed);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Подписчик Kafka подтверждённых броней остановлен");
        }
        finally
        {
            consumer.Close();
        }

    }

    /// <summary>
    /// Десериализует сообщение и резервирует места в отдельной области зависимостей.
    /// </summary>
    private async Task ProcessMessageAsync(string payload, CancellationToken cancellationToken)
    {
        var bookingConfirmed = JsonSerializer.Deserialize<BookingConfirmed>(payload);
        if (bookingConfirmed is null)
        {
            logger.LogWarning("Пропущено Kafka-сообщение с пустым или неверным телом");

            return;
        }

        if (bookingConfirmed.SeatsCount <= 0)
        {
            logger.LogWarning(
                "Пропущено подтверждение брони {BookingId}: число мест {SeatsCount} некорректно",
                bookingConfirmed.BookingId,
                bookingConfirmed.SeatsCount);

            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventEntity = await repository.GetForUpdateAsync(bookingConfirmed.EventId, cancellationToken);
        if (eventEntity is null)
        {
            logger.LogWarning(
                "Пропущено подтверждение брони {BookingId}: событие {EventId} не найдено",
                bookingConfirmed.BookingId,
                bookingConfirmed.EventId);

            return;
        }

        if (!eventEntity.TryReserveSeats(bookingConfirmed.SeatsCount))
        {
            logger.LogWarning(
                "Пропущено подтверждение брони {BookingId}: для события {EventId} нет свободных мест",
                bookingConfirmed.BookingId,
                bookingConfirmed.EventId);

            return;
        }

        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Для события {EventId} зарезервировано мест: {SeatsCount} по брони {BookingId}",
            bookingConfirmed.EventId,
            bookingConfirmed.SeatsCount,
            bookingConfirmed.BookingId);
    }
}
