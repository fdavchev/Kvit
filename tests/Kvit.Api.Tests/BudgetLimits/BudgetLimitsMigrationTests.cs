using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Postgres;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.BudgetLimits
{
    public class BudgetLimitsMigrationTests(PostgresFixture _postgres)
    {
        private const string LastMigrationBeforeTheLimits = "20261024094626_BudgetTotalCurrency";

        [Fact]
        public async Task Migrate_UsersThatExistedBeforeTheTable_HaveNoLimitRows()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTheLimits, TestContext.Current.CancellationToken);
            Guid userId = await InsertOldUserAsync(connectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Equal(0, await CountLimitRowsAsync(connectionString));
            Assert.Equal(1, await CountUsersAsync(connectionString, userId));
        }

        [Fact]
        public async Task Migrate_TableDoesNotExistBeforeTheMigrationAndExistsAfterIt()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTheLimits, TestContext.Current.CancellationToken);
            Assert.False(await TableExistsAsync(connectionString));

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.True(await TableExistsAsync(connectionString));
        }

        [Fact]
        public async Task Migrate_BudgetLimits_IsInTheMigrationHistoryExactlyOnceAfterBudgetTotalCurrency()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
            Assert.Single(applied, migration => migration.EndsWith("_BudgetLimits", StringComparison.Ordinal));
            int previous = applied.IndexOf(LastMigrationBeforeTheLimits);
            int limits = applied.FindIndex(migration => migration.EndsWith("_BudgetLimits", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The BudgetTotalCurrency migration is not applied.");
            Assert.True(limits > previous, $"BudgetLimits is at {limits} but BudgetTotalCurrency is at {previous}.");
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

        private static async Task<long> CountLimitRowsAsync(string connectionString)
        {
            return await ScalarAsync(connectionString, "SELECT count(*) FROM budget_limits");
        }

        private static async Task<long> CountUsersAsync(string connectionString, Guid userId)
        {
            return await ScalarAsync(connectionString, $"SELECT count(*) FROM users WHERE id = '{userId}'");
        }

        private static async Task<bool> TableExistsAsync(string connectionString)
        {
            return await ScalarAsync(connectionString, "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'budget_limits'") == 1;
        }

        private static async Task<long> ScalarAsync(string connectionString, string sql)
        {
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(sql, connection);

            return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        }
    }
}
