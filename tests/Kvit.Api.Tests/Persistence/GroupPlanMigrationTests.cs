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
    public class GroupPlanMigrationTests(PostgresFixture _postgres)
    {
        private const string LastMigrationBeforeThePlan = "20261027092926_DuplicateAnswers";

        [Fact]
        public async Task Migrate_GroupsThatExistedBeforeThePlan_HaveNoPlan()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeThePlan, TestContext.Current.CancellationToken);
            Guid groupId = await InsertOldGroupAsync(connectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.Equal("none", await ReadPlanAsync(connectionString, groupId));
        }

        private static async Task<Guid> InsertOldGroupAsync(string connectionString)
        {
            Guid ownerUserId = Guid.CreateVersion7();
            Guid groupId = Guid.CreateVersion7();
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(
                "SET session_replication_role = replica; "
                + "INSERT INTO groups (id, kind, name, emoji, default_currency, status, owner_user_id, invite_token, invite_token_created_at, created_by_user_id, created_at) "
                + "VALUES (@group_id, 'Group', 'Old group', 'x', 'MKD', 'Open', @owner, 'old-plan-token', now(), @owner, now());",
                connection);
            command.Parameters.AddWithValue("group_id", groupId);
            command.Parameters.AddWithValue("owner", ownerUserId);

            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

            return groupId;
        }

        private static async Task<string> ReadPlanAsync(string connectionString, Guid groupId)
        {
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(
                "SELECT CASE WHEN plan_amount_minor IS NULL AND plan_currency IS NULL THEN 'none' ELSE 'has a plan' END FROM groups WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", groupId);

            object? value = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

            return (string)(value ?? throw new InvalidOperationException($"No group found for {groupId}."));
        }
    }
}
