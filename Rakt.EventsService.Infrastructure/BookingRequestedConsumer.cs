using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rakt.Contracts.Messaging;
using Rakt.EventsService.Application;
using Rakt.EventsService.Domain;

namespace Rakt.EventsService.Infrastructure;

/// <summary>
/// Резервирует места по запросам сервиса броней и публикует результат.
/// </summary>
public sealed class BookingRequestedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ICache cache,
    CacheOptions cacheOptions,
    ILogger<BookingRequestedConsumer> logger) : BackgroundService
{
    /// <summary>
    /// Читает запросы бронирования из Kafka до остановки приложения.
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
        consumer.Subscribe(BookingTopics.Requested);
        logger.LogInformation("Подписчик Kafka запущен для топика {TopicName}", BookingTopics.Requested);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    await ProcessMessageAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (ConsumeException exception)
                {
                    logger.LogWarning(
                        exception,
                        "Топик запросов брони ещё недоступен. Повторная попытка будет выполнена позже.");
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
                        "Не удалось идемпотентно обработать запрос брони. Сообщение будет прочитано повторно.");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Подписчик Kafka запросов брони остановлен");
        }
        finally
        {
            consumer.Close();
        }
    }

    /// <summary>
    /// Резервирует место либо публикует отказ для одной брони.
    /// </summary>
    private async Task ProcessMessageAsync(string payload, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<BookingRequested>(payload);
        if (request is null)
        {
            logger.LogWarning("Пропущен запрос брони с пустым или неверным телом");

            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<ISeatsReservationPublisher>();
        var reservation = await repository.GetReservationAsync(request.BookingId, cancellationToken);
        if (reservation?.IsCancelled == true)
        {
            logger.LogInformation(
                "Пропущен запрос отменённой брони {BookingId}",
                request.BookingId);

            return;
        }

        if (reservation?.IsSeatReserved == true)
        {
            await publisher.PublishAsync(
                new SeatsReserved(request.BookingId, request.EventId, DateTimeOffset.UtcNow),
                cancellationToken);

            return;
        }

        var eventEntity = await repository.GetForUpdateAsync(request.EventId, cancellationToken);

        if (eventEntity is null)
        {
            await publisher.PublishAsync(
                new SeatsReservationRejected(
                    request.BookingId,
                    request.EventId,
                    "Событие не найдено.",
                    DateTimeOffset.UtcNow),
                cancellationToken);

            return;
        }

        if (!eventEntity.TryReserveSeat())
        {
            await publisher.PublishAsync(
                new SeatsReservationRejected(
                    request.BookingId,
                    request.EventId,
                    "Нет свободных мест.",
                    DateTimeOffset.UtcNow),
                cancellationToken);

            return;
        }

        await repository.AddReservationAsync(
            BookingSeatReservation.CreateReserved(request.BookingId, request.EventId),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await UpdateEventCacheAsync(eventEntity);
        await publisher.PublishAsync(
            new SeatsReserved(request.BookingId, request.EventId, DateTimeOffset.UtcNow),
            cancellationToken);
        logger.LogInformation(
            "Место события {EventId} зарезервировано по брони {BookingId}",
            request.EventId,
            request.BookingId);
    }

    /// <summary>
    /// Обновляет кеш события после успешного резервирования места в базе данных.
    /// </summary>
    private Task UpdateEventCacheAsync(Event eventEntity) =>
        cache.SetAsync(
            $"event:{eventEntity.Id}",
            JsonSerializer.Serialize(EventInfoDto.FromEntity(eventEntity)),
            TimeSpan.FromMinutes(cacheOptions.EventTimeToLiveMinutes));
}
