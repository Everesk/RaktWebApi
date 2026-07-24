using Microsoft.EntityFrameworkCore;

namespace Rakt.IntegrationTests;

/// <summary>
/// Интеграционные тесты схемы, создаваемой миграциями EF Core.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MigrationSchemaTests(PostgreSqlFixture fixture) : PostgreSqlTestBase(fixture)
{
    /// <summary>
    /// Проверяет создание таблиц, первичных ключей и внешних ключей бронирования миграциями.
    /// </summary>
    [Fact]
    public async Task Migrations_CreateExpectedTablesAndConstraints()
    {
        await using var context = Fixture.CreateDbContext();
        await context.Database.OpenConnectionAsync();

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.table_constraints AS constraints
            JOIN information_schema.key_column_usage AS key_columns
                ON constraints.constraint_name = key_columns.constraint_name
                AND constraints.table_schema = key_columns.table_schema
            LEFT JOIN information_schema.constraint_column_usage AS referenced_columns
                ON constraints.constraint_name = referenced_columns.constraint_name
                AND constraints.table_schema = referenced_columns.table_schema
            WHERE (constraints.constraint_name = 'pk_events'
                    AND constraints.constraint_type = 'PRIMARY KEY'
                    AND key_columns.table_name = 'events'
                    AND key_columns.column_name = 'id')
               OR (constraints.constraint_name = 'pk_bookings'
                    AND constraints.constraint_type = 'PRIMARY KEY'
                    AND key_columns.table_name = 'bookings'
                    AND key_columns.column_name = 'id')
               OR (constraints.constraint_name = 'pk_users'
                    AND constraints.constraint_type = 'PRIMARY KEY'
                    AND key_columns.table_name = 'users'
                    AND key_columns.column_name = 'id')
               OR (constraints.constraint_name = 'fk_bookings_events_event_id'
                    AND constraints.constraint_type = 'FOREIGN KEY'
                    AND key_columns.table_name = 'bookings'
                    AND key_columns.column_name = 'event_id'
                    AND referenced_columns.table_name = 'events'
                    AND referenced_columns.column_name = 'id')
               OR (constraints.constraint_name = 'fk_bookings_users_user_id'
                    AND constraints.constraint_type = 'FOREIGN KEY'
                    AND key_columns.table_name = 'bookings'
                    AND key_columns.column_name = 'user_id'
                    AND referenced_columns.table_name = 'users'
                    AND referenced_columns.column_name = 'id');
            """;

        var constraintsCount = Convert.ToInt32(await command.ExecuteScalarAsync());

        Assert.Equal(5, constraintsCount);

        command.CommandText = """
            SELECT COUNT(*)
            FROM pg_indexes
            WHERE schemaname = current_schema()
              AND tablename = 'users'
              AND indexname = 'ux_users_login'
              AND indexdef LIKE 'CREATE UNIQUE INDEX%';
            """;

        var uniqueLoginIndexesCount = Convert.ToInt32(await command.ExecuteScalarAsync());

        Assert.Equal(1, uniqueLoginIndexesCount);
    }
}
