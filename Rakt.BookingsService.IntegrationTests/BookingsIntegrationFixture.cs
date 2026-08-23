using Microsoft.EntityFrameworkCore;
using Rakt.BookingsService.Infrastructure;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace Rakt.BookingsService.IntegrationTests;

/// <summary>
/// Управляет PostgreSQL и Kafka для интеграционных тестов сервиса броней.
/// </summary>
public sealed class BookingsIntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("rakt_bookings_integration_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly KafkaContainer _kafkaContainer = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.5.0")
        .Build();

    /// <summary>
    /// Адрес Kafka-брокера тестового контейнера.
    /// </summary>
    public string KafkaBootstrapServers => _kafkaContainer.GetBootstrapAddress();

    /// <summary>
    /// Запускает PostgreSQL и Kafka.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await _kafkaContainer.StartAsync();
    }

    /// <summary>
    /// Удаляет содержимое базы и применяет миграции сервиса броней.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Создаёт контекст данных броней с перехватчиком времени создания.
    /// </summary>
    public BookingsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString())
            .AddInterceptors(new BookingCreatedAtInterceptor())
            .Options;

        return new BookingsDbContext(options);
    }

    /// <summary>
    /// Останавливает и удаляет тестовые контейнеры.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _kafkaContainer.DisposeAsync();
        await _postgreSqlContainer.DisposeAsync();
    }
}
