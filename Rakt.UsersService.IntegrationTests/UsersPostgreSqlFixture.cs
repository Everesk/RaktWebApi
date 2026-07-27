using Microsoft.EntityFrameworkCore;
using Rakt.UsersService.Infrastructure;
using Testcontainers.PostgreSql;

namespace Rakt.UsersService.IntegrationTests;

/// <summary>
/// Управляет контейнером PostgreSQL для интеграционных тестов пользователей.
/// </summary>
public sealed class UsersPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("rakt_users_integration_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    /// <summary>
    /// Запускает контейнер PostgreSQL.
    /// </summary>
    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    /// <summary>
    /// Удаляет содержимое базы и применяет миграции сервиса пользователей.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Создаёт контекст данных пользователей.
    /// </summary>
    public UsersDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new UsersDbContext(options);
    }

    /// <summary>
    /// Останавливает и удаляет контейнер PostgreSQL.
    /// </summary>
    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
