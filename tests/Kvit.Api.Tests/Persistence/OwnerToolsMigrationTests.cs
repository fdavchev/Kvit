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
    public class OwnerToolsMigrationTests(PostgresFixture _postgres)
    {
        private const string LastMigrationBeforeTheOwnerTools = "20261108181057_AccountDeletion";

        [Fact]
        public async Task Migrate_UsersThatExistedBeforeTheColumn_HaveSuspendedAtNull()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTheOwnerTools, TestContext.Current.CancellationToken);
            Guid userId = await InsertOldUserAsync(connectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Equal("null", await ReadTextAsync(connectionString, "SELECT COALESCE(suspended_at::text, 'null') FROM users WHERE id = @id", userId));
        }

        [Fact]
        public async Task Migrate_UsersThatExistedBeforeTheColumn_KeepTheirNameAndEmail()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTheOwnerTools, TestContext.Current.CancellationToken);
            Guid userId = await InsertOldUserAsync(connectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Equal("Old user|old@example.com", await ReadTextAsync(connectionString, "SELECT display_name || '|' || email FROM users WHERE id = @id", userId));
        }

        [Fact]
        public async Task Migrate_GroupsThatExistedBeforeTheColumn_HaveAdminClosedAtNullAndKeepTheirNameAndStatus()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTheOwnerTools, TestContext.Current.CancellationToken);
            Guid ownerId = await InsertOldUserAsync(connectionString);
            Guid groupId = await InsertOldGroupAsync(connectionString, ownerId);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Equal(
                "Old trip|Finished|null",
                await ReadTextAsync(connectionString, "SELECT name || '|' || status || '|' || COALESCE(admin_closed_at::text, 'null') FROM groups WHERE id = @id", groupId));
        }

        [Fact]
        public async Task Migrate_OwnerTools_IsInTheMigrationHistoryExactlyOnceAfterAccountDeletion()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
            Assert.Single(applied, migration => migration.EndsWith("_OwnerTools", StringComparison.Ordinal));
            int previous = applied.IndexOf(LastMigrationBeforeTheOwnerTools);
            int ownerTools = applied.FindIndex(migration => migration.EndsWith("_OwnerTools", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The AccountDeletion migration is not applied.");
            Assert.True(ownerTools > previous, $"OwnerTools is at {ownerTools} but AccountDeletion is at {previous}.");
        }

        [Fact]
        public async Task Migrate_OwnerTools_CreatesAnEmptyActionLog()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Equal("0", await ReadTextAsync(connectionString, "SELECT count(*)::text FROM admin_actions"));
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

        private static async Task<Guid> InsertOldGroupAsync(string connectionString, Guid ownerUserId)
        {
            Guid groupId = Guid.CreateVersion7();
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(
                "INSERT INTO groups (id, kind, name, emoji, default_currency, status, owner_user_id, invite_token, invite_token_created_at, created_by_user_id, created_at) "
                + "VALUES (@id, 'Group', 'Old trip', 'x', 'MKD', 'Finished', @owner, @token, now(), @owner, now())",
                connection);
            command.Parameters.AddWithValue("id", groupId);
            command.Parameters.AddWithValue("owner", ownerUserId);
            command.Parameters.AddWithValue("token", $"token-{groupId:N}");

            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

            return groupId;
        }

        private static async Task<string> ReadTextAsync(string connectionString, string sql, Guid? id = null)
        {
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(sql, connection);
            if (id is not null)
            {
                command.Parameters.AddWithValue("id", id.Value);
            }

            object? value = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

            return (string)(value ?? throw new InvalidOperationException($"The query returned no row: {sql}"));
        }
    }
}
