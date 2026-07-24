using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RaktApi.Infrastructure.Repositories;

namespace Rakt.IntegrationTests;

/// <summary>
/// Интеграционные тесты репозитория пользователей на PostgreSQL.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserRepositoryTests(PostgreSqlFixture fixture) : PostgreSqlTestBase(fixture)
{
    /// <summary>
    /// Проверяет, что пользователь с некорректной ролью не нарушает вход и игнорируется.
    /// </summary>
    [Fact]
    public async Task GetByLoginAsync_ShouldIgnoreUserWithInvalidRole()
    {
        // Arrange
        const string login = "broken-user";
        await using var context = Fixture.CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO users (id, login, password_hash, role) VALUES ({0}, {1}, {2}, {3})",
            Guid.NewGuid(),
            login,
            "password-hash",
            "BrokenRole");
        var repository = new UserRepository(context, NullLogger<UserRepository>.Instance);

        // Act
        var user = await repository.GetByLoginAsync(login);

        // Assert
        Assert.Null(user);
    }
}
