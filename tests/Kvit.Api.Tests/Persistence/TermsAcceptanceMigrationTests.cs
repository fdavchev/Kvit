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
    public class TermsAcceptanceMigrationTests(PostgresFixture _postgres)
    {
        private const string LastMigrationBeforeTheTerms = "20261027124121_GroupPlan";

        [Fact]
        public async Task Migrate_UsersThatExistedBeforeTheColumns_HaveBothTermsColumnsNull()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTheTerms, TestContext.Current.CancellationToken);
            Guid userId = await InsertOldUserAsync(connectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Equal("null|null", await ReadTermsAsync(connectionString, userId));
        }

        [Fact]
        public async Task Migrate_TermsAcceptance_IsInTheMigrationHistoryExactlyOnceAfterGroupPlan()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
            Assert.Single(applied, migration => migration.EndsWith("_TermsAcceptance", StringComparison.Ordinal));
            int previous = applied.IndexOf(LastMigrationBeforeTheTerms);
            int terms = applied.FindIndex(migration => migration.EndsWith("_TermsAcceptance", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The GroupPlan migration is not applied.");
            Assert.True(terms > previous, $"TermsAcceptance is at {terms} but GroupPlan is at {previous}.");
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

        private static async Task<string> ReadTermsAsync(string connectionString, Guid userId)
        {
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(
                "SELECT COALESCE(terms_version, 'null') || '|' || COALESCE(terms_accepted_at::text, 'null') FROM users WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", userId);

            object? value = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

            return (string)(value ?? throw new InvalidOperationException($"No user found for {userId}."));
        }
    }
}
