using System.Globalization;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public static class CleanupRows
    {
        private const string DeletedGroupIds = "SELECT id FROM groups WHERE deleted_at IS NOT NULL";

        public static Task RemoveSoftDeletedRowsAsync(MigratedDatabase database)
        {
            return database.ExecuteAsync(
                "DELETE FROM duplicate_answers WHERE personal_entry_id IN (SELECT id FROM personal_entries WHERE deleted_at IS NOT NULL) "
                + $"OR expense_id IN (SELECT id FROM expenses WHERE deleted_at IS NOT NULL OR group_id IN ({DeletedGroupIds})); "
                + "DELETE FROM personal_entries WHERE deleted_at IS NOT NULL; "
                + $"DELETE FROM expenses WHERE deleted_at IS NOT NULL OR group_id IN ({DeletedGroupIds}); "
                + $"DELETE FROM settlements WHERE deleted_at IS NOT NULL OR group_id IN ({DeletedGroupIds}); "
                + $"DELETE FROM closing_responses WHERE group_id IN ({DeletedGroupIds}); "
                + $"DELETE FROM activity_events WHERE group_id IN ({DeletedGroupIds}); "
                + $"DELETE FROM group_members WHERE group_id IN ({DeletedGroupIds}); "
                + "DELETE FROM groups WHERE deleted_at IS NOT NULL");
        }

        public static Task RemoveAllDuplicateAnswersAsync(MigratedDatabase database)
        {
            return database.ExecuteAsync("DELETE FROM duplicate_answers");
        }

        public static Task ForgetPendingDeletionRequestsAsync(MigratedDatabase database)
        {
            return database.ExecuteAsync(
                "UPDATE users SET deletion_requested_at = NULL, deletion_name_choice = NULL "
                + "WHERE purged_at IS NULL AND deletion_requested_at IS NOT NULL");
        }

        public static Task DetachAllUsageEventsAsync(MigratedDatabase database)
        {
            return database.ExecuteAsync("UPDATE usage_events SET user_id = NULL WHERE user_id IS NOT NULL");
        }

        public static Task SetExpenseDeletedAtAsync(MigratedDatabase database, Guid expenseId, Guid deletedByUserId, DateTimeOffset deletedAt)
        {
            return SetDeletedAtAsync(database, "expenses", expenseId, deletedByUserId, deletedAt);
        }

        public static Task SetSettlementDeletedAtAsync(MigratedDatabase database, Guid settlementId, Guid deletedByUserId, DateTimeOffset deletedAt)
        {
            return SetDeletedAtAsync(database, "settlements", settlementId, deletedByUserId, deletedAt);
        }

        public static Task SetGroupDeletedAtAsync(MigratedDatabase database, Guid groupId, Guid deletedByUserId, DateTimeOffset deletedAt)
        {
            return SetDeletedAtAsync(database, "groups", groupId, deletedByUserId, deletedAt);
        }

        public static async Task<long> InsertUsageEventAsync(MigratedDatabase database, Guid? userId, string type, DateTimeOffset occurredAt, string? detail = null)
        {
            List<string?> ids = await database.QueryAsync(
                "INSERT INTO usage_events (user_id, type, detail, occurred_at, occurred_on) VALUES (@user_id, @type, @detail, @occurred_at, @occurred_on) RETURNING id::text",
                new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = (object?)userId ?? DBNull.Value },
                new NpgsqlParameter("type", type),
                new NpgsqlParameter("detail", NpgsqlDbType.Text) { Value = (object?)detail ?? DBNull.Value },
                new NpgsqlParameter("occurred_at", occurredAt),
                new NpgsqlParameter("occurred_on", DateOnly.FromDateTime(occurredAt.UtcDateTime)));

            return long.Parse(Assert.Single(ids)!, CultureInfo.InvariantCulture);
        }

        public static async Task<Guid?> UsageEventUserIdAsync(MigratedDatabase database, long usageEventId)
        {
            List<string?> values = await database.QueryAsync(
                "SELECT user_id::text FROM usage_events WHERE id = @id",
                new NpgsqlParameter("id", usageEventId));
            string? userId = Assert.Single(values);

            return userId is null ? null : Guid.Parse(userId);
        }

        public static async Task<string> UsageEventContentAsync(MigratedDatabase database, long usageEventId)
        {
            List<string?> values = await database.QueryAsync(
                "SELECT concat_ws('|', id, type, coalesce(detail, '-'), occurred_at, occurred_on) FROM usage_events WHERE id = @id",
                new NpgsqlParameter("id", usageEventId));

            return Assert.Single(values) ?? throw new InvalidOperationException($"The usage event {usageEventId} came back as null.");
        }

        public static async Task<int> CountUsageEventsAsync(MigratedDatabase database)
        {
            List<string?> counts = await database.QueryAsync("SELECT count(*)::text FROM usage_events");

            return int.Parse(Assert.Single(counts)!, CultureInfo.InvariantCulture);
        }

        public static async Task<int> CountRowsWithIdAsync(MigratedDatabase database, string table, Guid id)
        {
            List<string?> counts = await database.QueryAsync(
                $"SELECT count(*)::text FROM {table} WHERE id = @id",
                new NpgsqlParameter("id", id));

            return int.Parse(Assert.Single(counts)!, CultureInfo.InvariantCulture);
        }

        public static async Task<GroupRowCounts> CountRowsOfGroupAsync(MigratedDatabase database, Guid groupId)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT concat_ws(',', "
                + "(SELECT count(*) FROM groups WHERE id = @group_id), "
                + "(SELECT count(*) FROM group_members WHERE group_id = @group_id), "
                + "(SELECT count(*) FROM expenses WHERE group_id = @group_id), "
                + "(SELECT count(*) FROM expense_shares WHERE expense_id IN (SELECT id FROM expenses WHERE group_id = @group_id)), "
                + "(SELECT count(*) FROM settlements WHERE group_id = @group_id), "
                + "(SELECT count(*) FROM closing_responses WHERE group_id = @group_id), "
                + "(SELECT count(*) FROM activity_events WHERE group_id = @group_id))",
                new NpgsqlParameter("group_id", groupId));
            int[] counts = [.. Assert.Single(rows)!.Split(',').Select(count => int.Parse(count, CultureInfo.InvariantCulture))];

            return new GroupRowCounts(counts[0], counts[1], counts[2], counts[3], counts[4], counts[5], counts[6]);
        }

        public static async Task<int> CountDuplicateAnswersAsync(MigratedDatabase database)
        {
            List<string?> counts = await database.QueryAsync("SELECT count(*)::text FROM duplicate_answers");

            return int.Parse(Assert.Single(counts)!, CultureInfo.InvariantCulture);
        }

        public static async Task<string> SnapshotOfTheWholeDatabaseAsync(MigratedDatabase database)
        {
            string everyTableExceptUsers = await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(database);
            string users = await DeletionRows.SnapshotOfTablesAsync(database, "users");

            return $"{everyTableExceptUsers}\n{users}";
        }

        private static Task SetDeletedAtAsync(MigratedDatabase database, string table, Guid id, Guid deletedByUserId, DateTimeOffset deletedAt)
        {
            return database.ExecuteAsync(
                $"UPDATE {table} SET deleted_at = @deleted_at, deleted_by_user_id = @deleted_by WHERE id = @id",
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("deleted_at", deletedAt),
                new NpgsqlParameter("deleted_by", deletedByUserId));
        }
    }
}
