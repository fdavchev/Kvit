using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Postgres;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.BudgetCurrency
{
    public class BudgetTotalCurrencyMigrationTests(PostgresFixture _postgres)
    {
        private const string LastMigrationBeforeTheCurrency = "20261024090302_BudgetShare";

        [Fact]
        public async Task Migrate_UsersThatExistedBeforeTheColumn_GetNull()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTheCurrency, TestContext.Current.CancellationToken);
            Guid userId = await InsertOldUserAsync(connectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Null(await ReadBudgetTotalCurrencyAsync(connectionString, userId));
        }

        [Fact]
        public async Task Migrate_BudgetTotalCurrency_IsInTheMigrationHistoryExactlyOnceAfterBudgetShare()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
            Assert.Single(applied, migration => migration.EndsWith("_BudgetTotalCurrency", StringComparison.Ordinal));
            int previous = applied.IndexOf(LastMigrationBeforeTheCurrency);
            int currency = applied.FindIndex(migration => migration.EndsWith("_BudgetTotalCurrency", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The BudgetShare migration is not applied.");
            Assert.True(currency > previous, $"BudgetTotalCurrency is at {currency} but BudgetShare is at {previous}.");
        }

        private static async Task<Guid> InsertOldUserAsync(string connectionString)
        {
            Guid userId = Guid.CreateVersion7();
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(
                "INSERT INTO users (id, user_name, display_name, language, time_zone, created_at, access_failed_count, email_confirmed, lockout_enabled, phone_number_confirmed, two_factor_enabled) "
                + "VALUES (@id, 'old@example.com', 'Old user', 'en', 'Europe/Skopje', now(), 0, false, false, false, false)",
                connection);
            command.Parameters.AddWithValue("id", userId);

            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

            return userId;
        }

        private static async Task<string?> ReadBudgetTotalCurrencyAsync(string connectionString, Guid userId)
        {
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new("SELECT budget_total_currency FROM users WHERE id = @id", connection);
            command.Parameters.AddWithValue("id", userId);

            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken), $"No user row found for {userId}.");

            return reader.IsDBNull(0) ? null : reader.GetString(0);
        }
    }
}
