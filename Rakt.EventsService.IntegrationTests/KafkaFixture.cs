using Testcontainers.Kafka;

namespace Rakt.EventsService.IntegrationTests;

/// <summary>
/// Управляет Kafka-контейнером для интеграционных тестов сервиса событий.
/// </summary>
public sealed class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.5.0")
        .Build();

    /// <summary>
    /// Адрес Kafka-брокера тестового контейнера.
    /// </summary>
    public string BootstrapServers => _container.GetBootstrapAddress();

    /// <summary>
    /// Запускает Kafka-контейнер.
    /// </summary>
    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    /// <summary>
    /// Останавливает и удаляет Kafka-контейнер.
    /// </summary>
    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
