using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rakt.BookingsService.Domain;
using Rakt.BookingsService.Infrastructure;
using Rakt.Contracts.Messaging;

namespace Rakt.BookingsService.IntegrationTests;

/// <summary>
/// Интеграционные тесты сервиса броней.
/// </summary>
[Trait("Category", "Integration")]
public sealed class BookingsServiceIntegrationTests(BookingsIntegrationFixture fixture)
    : IClassFixture<BookingsIntegrationFixture>, IAsyncLifetime
{
    /// <summary>
    /// Подготавливает чистую базу броней.
    /// </summary>
    public Task InitializeAsync()
    {
        return fixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// Не требует действий после теста.
    /// </summary>
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Проверяет миграции таблицы броней и отсутствие внешних ключей других сервисов.
    /// </summary>
    [Fact]
    public async Task Migrations_CreateBookingsTableWithoutCrossServiceForeignKeys()
    {
        await using var context = fixture.CreateDbContext();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        var columns = await context.Database
            .SqlQueryRaw<string>("SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_name = 'bookings'")
            .ToListAsync();
        var foreignKeysCount = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM information_schema.table_constraints WHERE table_name = 'bookings' AND constraint_type = 'FOREIGN KEY'")
            .SingleAsync();

        Assert.Equal(2, appliedMigrations.Count());
        Assert.Contains("CreatedAt", columns);
        Assert.Contains("ProcessedAt", columns);
        Assert.Equal(0, foreignKeysCount);
    }

    /// <summary>
    /// Проверяет сохранение брони без сущностей пользователей и событий в этой базе.
    /// </summary>
    [Fact]
    public async Task Repository_PersistsBookingWithoutForeignEntities()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new BookingRepository(context);
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

        await repository.AddAsync(booking);
        await repository.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var storedBooking = await repository.GetAsync(booking.Id);

        Assert.NotNull(storedBooking);
        Assert.Equal(booking.UserId, storedBooking.UserId);
        Assert.Equal(booking.EventId, storedBooking.EventId);
        Assert.Equal(BookingStatus.Pending, storedBooking.Status);
        Assert.NotEqual(default, storedBooking.CreatedAt);
    }

    /// <summary>
    /// Проверяет публикацию запроса брони в Kafka с ключом идентификатора события.
    /// </summary>
    [Fact]
    public async Task KafkaPublisher_PublishesBookingRequestedWithEventIdKey()
    {
        await CreateRequestedTopicAsync();

        var consumerConfiguration = new ConsumerConfig
        {
            BootstrapServers = fixture.KafkaBootstrapServers,
            GroupId = $"bookings-integration-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        using var consumer = new ConsumerBuilder<string, string>(consumerConfiguration).Build();
        consumer.Subscribe(BookingTopics.Requested);

        using var publisher = new KafkaBookingMessagePublisher(
            Options.Create(new KafkaOptions
            {
                BootstrapServers = fixture.KafkaBootstrapServers
            }));
        var bookingRequested = new BookingRequested(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        await publisher.PublishAsync(bookingRequested);

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var consumedMessage = await ConsumeMessageAsync(
            consumer,
            bookingRequested.BookingId,
            cancellationTokenSource.Token);
        var payload = JsonSerializer.Deserialize<BookingRequested>(consumedMessage.Message.Value);

        Assert.Equal(bookingRequested.EventId.ToString(), consumedMessage.Message.Key);
        Assert.NotNull(payload);
        Assert.Equal(bookingRequested.BookingId, payload.BookingId);
        Assert.Equal(bookingRequested.EventId, payload.EventId);
        Assert.Equal(bookingRequested.UserId, payload.UserId);
    }

    /// <summary>
    /// Создаёт топик запросов брони для изолированной проверки издателя.
    /// </summary>
    private async Task CreateRequestedTopicAsync()
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = fixture.KafkaBootstrapServers
        }).Build();

        try
        {
            await adminClient.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name = BookingTopics.Requested,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            ]);
        }
        catch (CreateTopicsException exception) when (
            exception.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
        }
    }

    /// <summary>
    /// Возвращает сообщение нужной брони, игнорируя сообщения предыдущих тестов.
    /// </summary>
    private static async Task<ConsumeResult<string, string>> ConsumeMessageAsync(
        IConsumer<string, string> consumer,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = consumer.Consume(cancellationToken);
            var payload = JsonSerializer.Deserialize<BookingRequested>(result.Message.Value);

            if (payload?.BookingId == bookingId)
            {
                return result;
            }

            await Task.Yield();
        }

        throw new TimeoutException("Не получено событие о подтверждении брони из Kafka.");
    }
}
