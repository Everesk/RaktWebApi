using Microsoft.EntityFrameworkCore;
using Rakt.UsersService.Domain;
using Rakt.UsersService.Infrastructure;

namespace Rakt.UsersService.IntegrationTests;

/// <summary>
/// Интеграционные тесты сервиса пользователей.
/// </summary>
[Trait("Category", "Integration")]
public sealed class UsersServiceIntegrationTests(UsersPostgreSqlFixture fixture)
    : IClassFixture<UsersPostgreSqlFixture>, IAsyncLifetime
{
    /// <summary>
    /// Подготавливает чистую базу пользователей.
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
    /// Проверяет таблицу и уникальный индекс, созданные миграцией.
    /// </summary>
    [Fact]
    public async Task Migrations_CreateUsersTableAndUniqueLoginIndex()
    {
        await using var context = fixture.CreateDbContext();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        var hasTable = await context.Database
            .SqlQueryRaw<bool>("SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'users') AS \"Value\"")
            .SingleAsync();
        var hasIndex = await context.Database
            .SqlQueryRaw<bool>("SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'users' AND indexname = 'IX_users_Login') AS \"Value\"")
            .SingleAsync();

        Assert.Single(appliedMigrations);
        Assert.True(hasTable);
        Assert.True(hasIndex);
    }

    /// <summary>
    /// Проверяет сохранение пользователя и поиск по логину.
    /// </summary>
    [Fact]
    public async Task Repository_PersistsAndFindsUserByLogin()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new UserRepository(context);
        var user = User.Create("integration-user", "password-hash");

        await repository.AddAsync(user);

        context.ChangeTracker.Clear();
        var storedUser = await repository.FindByLoginAsync(user.Login);

        Assert.NotNull(storedUser);
        Assert.Equal(user.Id, storedUser.Id);
        Assert.Equal(user.PasswordHash, storedUser.PasswordHash);
        Assert.Equal(user.Role, storedUser.Role);
    }
}
