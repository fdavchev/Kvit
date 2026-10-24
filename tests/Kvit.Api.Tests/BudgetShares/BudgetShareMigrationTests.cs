using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Postgres;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.BudgetShares
{
    public class BudgetShareMigrationTests(PostgresFixture _postgres)
    {
        private const string LastMigrationBeforeTheSwitch = "20261023210020_PersonalEntryOriginRules";

        [Fact]
        public async Task Migrate_RowsThatExistedBeforeTheSwitch_BecomeCounted()
        {
            string connectionString = _postgres.CreateDatabase();
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);
            using IServiceScope scope = factory.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.GetService<IMigrator>().MigrateAsync(LastMigrationBeforeTheSwitch, TestContext.Current.CancellationToken);
            (Guid memberId, Guid expenseId) = await InsertOneMemberWithOneShareAsync(connectionString);

            await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

            Assert.True(await ReadFlagAsync(connectionString, "SELECT counts_in_budget FROM group_members WHERE id = @id", memberId));
            Assert.True(await ReadFlagAsync(connectionString, "SELECT counts_in_budget FROM expense_shares WHERE expense_id = @id", expenseId));
        }

        private static async Task<(Guid MemberId, Guid ExpenseId)> InsertOneMemberWithOneShareAsync(string connectionString)
        {
            Guid ownerUserId = Guid.CreateVersion7();
            Guid groupId = Guid.CreateVersion7();
            Guid memberId = Guid.CreateVersion7();
            Guid expenseId = Guid.CreateVersion7();
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(
                "SET session_replication_role = replica; "
                + "INSERT INTO groups (id, kind, name, emoji, default_currency, status, owner_user_id, invite_token, invite_token_created_at, created_by_user_id, created_at) "
                + "VALUES (@group_id, 'Group', 'Old group', 'x', 'MKD', 'Open', @owner, 'old-token', now(), @owner, now()); "
                + "INSERT INTO group_members (id, group_id, user_id, name, joined_at, added_by_user_id) "
                + "VALUES (@member_id, @group_id, @owner, 'Ana', now(), @owner); "
                + "INSERT INTO expenses (id, group_id, amount_minor, currency, expense_date, paid_by_member_id, split_type, mkd_per_eur, rate_date, created_by_user_id, created_at, client_request_id) "
                + "VALUES (@expense_id, @group_id, 100000, 'MKD', '2026-10-05', @member_id, 'Equal', 61.5610, '2026-09-24', @owner, now(), @expense_id); "
                + "INSERT INTO expense_shares (expense_id, member_id, input_value, share_minor) VALUES (@expense_id, @member_id, 0, 100000);",
                connection);
            command.Parameters.AddWithValue("group_id", groupId);
            command.Parameters.AddWithValue("owner", ownerUserId);
            command.Parameters.AddWithValue("member_id", memberId);
            command.Parameters.AddWithValue("expense_id", expenseId);

            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

            return (memberId, expenseId);
        }

        private static async Task<bool> ReadFlagAsync(string connectionString, string sql, Guid id)
        {
            await using NpgsqlConnection connection = new(connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(sql, connection);
            command.Parameters.AddWithValue("id", id);

            object? value = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

            return (bool)(value ?? throw new InvalidOperationException($"No row found for {id}."));
        }
    }
}
