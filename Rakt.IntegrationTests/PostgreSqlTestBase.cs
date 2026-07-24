using RaktApi.Infrastructure.Data;
using RaktApi.Infrastructure.Repositories;

namespace Rakt.IntegrationTests;

/// <summary>
/// Базовый класс тестов репозиториев с чистой базой данных перед каждым тестом.
/// </summary>
public abstract class PostgreSqlTestBase(PostgreSqlFixture fixture) : IAsyncLifetime
{
    /// <summary>Общий контейнер PostgreSQL.</summary>
    protected PostgreSqlFixture Fixture { get; } = fixture;

    /// <summary>Очищает базу данных и применяет миграции перед тестом.</summary>
    public Task InitializeAsync() => Fixture.ResetDatabaseAsync();

    /// <summary>Не требует действий после отдельного теста.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Создаёт пару репозиториев в одном контексте для тестового сценария.</summary>
    protected RepositoryScope CreateRepositories()
    {
        var context = Fixture.CreateDbContext();
        return new RepositoryScope(context, new EventRepository(context), new BookingRepository(context), new UserRepository(context));
    }

    /// <summary>
    /// Хранит контекст и репозитории, использующие его в рамках одного теста.
    /// </summary>
    protected sealed class RepositoryScope(
        AppDbContext context,
        EventRepository events,
        BookingRepository bookings,
        UserRepository users) : IAsyncDisposable
    {
        /// <summary>Репозиторий событий.</summary>
        public EventRepository Events { get; } = events;

        /// <summary>Репозиторий бронирований.</summary>
        public BookingRepository Bookings { get; } = bookings;

        /// <summary>Репозиторий пользователей.</summary>
        public UserRepository Users { get; } = users;

        /// <summary>Освобождает контекст базы данных.</summary>
        public ValueTask DisposeAsync() => context.DisposeAsync();
    }
}
