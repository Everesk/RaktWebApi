using Confluent.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rakt.Contracts.Messaging;
using Rakt.EventsService.Infrastructure;

namespace Rakt.EventsService.IntegrationTests;

/// <summary>
/// Интеграционные проверки Kafka-инициализации сервиса событий.
/// </summary>
[Trait("Category", "Integration")]
public sealed class KafkaTopicInitializerTests(KafkaFixture fixture) : IClassFixture<KafkaFixture>
{
    /// <summary>
    /// Инициализатор должен создавать оба топика, потребляемые сервисом событий.
    /// </summary>
    [Fact]
    public async Task StartAsync_CreatesConfirmedAndCancelledTopics()
    {
        var initializer = new KafkaTopicInitializer(
            Options.Create(new KafkaOptions
            {
                BootstrapServers = fixture.BootstrapServers,
                ConsumerGroup = "events-integration-tests"
            }),
            NullLogger<KafkaTopicInitializer>.Instance);

        await initializer.StartAsync(CancellationToken.None);

        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = fixture.BootstrapServers
        }).Build();
        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));

        Assert.Contains(metadata.Topics, topic => topic.Topic == BookingTopics.Confirmed);
        Assert.Contains(metadata.Topics, topic => topic.Topic == BookingTopics.Cancelled);
    }

    /// <summary>
    /// После инициализации Kafka должна доставлять подтверждённое сообщение потребителю.
    /// </summary>
    [Fact]
    public async Task InitializedTopic_DeliversBookingConfirmedMessage()
    {
        var initializer = new KafkaTopicInitializer(
            Options.Create(new KafkaOptions
            {
                BootstrapServers = fixture.BootstrapServers,
                ConsumerGroup = "events-integration-tests"
            }),
            NullLogger<KafkaTopicInitializer>.Instance);
        await initializer.StartAsync(CancellationToken.None);

        var consumerConfiguration = new ConsumerConfig
        {
            BootstrapServers = fixture.BootstrapServers,
            GroupId = $"events-kafka-test-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        using var consumer = new ConsumerBuilder<string, string>(consumerConfiguration).Build();
        consumer.Subscribe(BookingTopics.Confirmed);
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = fixture.BootstrapServers
        }).Build();
        var bookingId = Guid.NewGuid();

        await producer.ProduceAsync(
            BookingTopics.Confirmed,
            new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = bookingId.ToString()
            });

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var result = consumer.Consume(cancellationTokenSource.Token);

        Assert.Equal(bookingId.ToString(), result.Message.Value);
    }
}
