using Microsoft.EntityFrameworkCore;
using RaktApi.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Rakt.IntegrationTests;

/// <summary>
/// Управляет единственным контейнером PostgreSQL для интеграционных тестов.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("rakt_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    /// <summary>
    /// Строка подключения к PostgreSQL, назначенная Testcontainers без фиксированного порта.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Запускает контейнер перед выполнением набора интеграционных тестов.
    /// </summary>
    public Task InitializeAsync() => _container.StartAsync();

    /// <summary>
    /// Удаляет базу, заново создаёт её и применяет все миграции для изоляции теста.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Создаёт контекст, подключённый к тестовому контейнеру.
    /// </summary>
    /// <returns>Контекст тестовой базы данных.</returns>
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Останавливает и удаляет контейнер после завершения всех тестов.
    /// </summary>
    public async Task DisposeAsync() => await _container.DisposeAsync();
}

/// <summary>
/// Объединяет тесты, использующие общий контейнер PostgreSQL, в непараллельную коллекцию.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    /// <summary>Имя коллекции интеграционных тестов PostgreSQL.</summary>
    public const string Name = "PostgreSql integration tests";
}
