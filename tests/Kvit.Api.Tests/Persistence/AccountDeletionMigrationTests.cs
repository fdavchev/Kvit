using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Postgres;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class AccountDeletionMigrationTests(PostgresFixture _postgres)
    {
        private const string LastMigrationBeforeTheDeletion = "20261104170728_TermsAcceptance";

        [Fact]
        public async Task Migrate_UsersThatExistedBeforeTheColumns_HaveAllThreeDeletionColumnsNull()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTheDeletion, TestContext.Current.CancellationToken);
            Guid userId = await InsertOldUserAsync(connectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Equal("null|null|null", await ReadDeletionAsync(connectionString, userId));
        }

        [Fact]
        public async Task Migrate_AccountDeletion_IsInTheMigrationHistoryExactlyOnceAfterTermsAcceptance()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
            Assert.Single(applied, migration => migration.EndsWith("_AccountDeletion", StringComparison.Ordinal));
            int previous = applied.IndexOf(LastMigrationBeforeTheDeletion);
            int deletion = applied.FindIndex(migration => migration.EndsWith("_AccountDeletion", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The TermsAcceptance migration is not applied.");
            Assert.True(deletion > previous, $"AccountDeletion is at {deletion} but TermsAcceptance is at {previous}.");
        }

        [Fact]
        public async Task Migrate_UsersThatExistedBeforeTheColumns_KeepTheirNameAndTerms()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTheDeletion, TestContext.Current.CancellationToken);
            Guid userId = await InsertOldUserAsync(connectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Equal("Old user|old@example.com", await ReadIdentityAsync(connectionString, userId));
        }

        private static async Task<Guid> InsertOldUserAsync(string connectionString)
        {
            Guid userId = Guid.CreateVersion7();
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(
                "INSERT INTO users (id, user_name, email, display_name, language, time_zone, created_at, access_failed_count, email_confirmed, lockout_enabled, phone_number_confirmed, two_factor_enabled) "
                + "VALUES (@id, 'old@example.com', 'old@example.com', 'Old user', 'en', 'Europe/Skopje', now(), 0, false, false, false, false)",
                connection);
            command.Parameters.AddWithValue("id", userId);

            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

            return userId;
        }

        private static async Task<string> ReadDeletionAsync(string connectionString, Guid userId)
        {
            return await ReadTextAsync(
                connectionString,
                userId,
                "SELECT COALESCE(deletion_requested_at::text, 'null') || '|' || COALESCE(deletion_name_choice, 'null') || '|' || COALESCE(purged_at::text, 'null') FROM users WHERE id = @id");
        }

        private static async Task<string> ReadIdentityAsync(string connectionString, Guid userId)
        {
            return await ReadTextAsync(connectionString, userId, "SELECT display_name || '|' || email FROM users WHERE id = @id");
        }

        private static async Task<string> ReadTextAsync(string connectionString, Guid userId, string sql)
        {
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(sql, connection);
            command.Parameters.AddWithValue("id", userId);

            object? value = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

            return (string)(value ?? throw new InvalidOperationException($"No user found for {userId}."));
        }
    }
}
