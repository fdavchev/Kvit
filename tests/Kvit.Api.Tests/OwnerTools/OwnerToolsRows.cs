using System.Globalization;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public static class OwnerToolsRows
    {
        public static Task<DateTimeOffset?> SuspendedAtAsync(MigratedDatabase database, Guid userId)
        {
            return DeletionRows.TimeOfAsync(database, "users WHERE id = @id", "suspended_at", new NpgsqlParameter("id", userId));
        }

        public static Task SetSuspendedAtAsync(MigratedDatabase database, Guid userId, DateTimeOffset? suspendedAt)
        {
            return database.ExecuteAsync(
                "UPDATE users SET suspended_at = @suspended_at WHERE id = @id",
                new NpgsqlParameter("id", userId),
                new NpgsqlParameter("suspended_at", NpgsqlDbType.TimestampTz) { Value = (object?)suspendedAt ?? DBNull.Value });
        }

        public static Task<DateTimeOffset?> AdminClosedAtAsync(MigratedDatabase database, Guid groupId)
        {
            return DeletionRows.TimeOfAsync(database, "groups WHERE id = @id", "admin_closed_at", new NpgsqlParameter("id", groupId));
        }

        public static Task SetAdminClosedAtAsync(MigratedDatabase database, Guid groupId, DateTimeOffset? adminClosedAt)
        {
            return database.ExecuteAsync(
                "UPDATE groups SET admin_closed_at = @admin_closed_at WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("admin_closed_at", NpgsqlDbType.TimestampTz) { Value = (object?)adminClosedAt ?? DBNull.Value });
        }

        public static Task SetCreatedAtAsync(MigratedDatabase database, Guid userId, DateTimeOffset createdAt)
        {
            return database.ExecuteAsync(
                "UPDATE users SET created_at = @created_at WHERE id = @id",
                new NpgsqlParameter("id", userId),
                new NpgsqlParameter("created_at", NpgsqlDbType.TimestampTz) { Value = createdAt });
        }

        public static Task SetEmailAsync(MigratedDatabase database, Guid userId, string email)
        {
            return database.ExecuteAsync(
                "UPDATE users SET email = @email, normalized_email = upper(@email) WHERE id = @id",
                new NpgsqlParameter("id", userId),
                new NpgsqlParameter("email", email));
        }

        public static Task SetGroupNameAsync(MigratedDatabase database, Guid groupId, string name)
        {
            return database.ExecuteAsync(
                "UPDATE groups SET name = @name WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("name", name));
        }

        public static Task InsertActionAsync(
            MigratedDatabase database,
            Guid adminUserId,
            string action,
            Guid? targetUserId,
            Guid? targetGroupId,
            string targetLabel = "Ana",
            string reason = "Reported by two members",
            DateTimeOffset? createdAt = null)
        {
            return database.ExecuteAsync(
                "INSERT INTO admin_actions (admin_user_id, action, target_user_id, target_group_id, target_label, reason, created_at) "
                + "VALUES (@admin_user_id, @action, @target_user_id, @target_group_id, @target_label, @reason, COALESCE(@created_at, now()))",
                new NpgsqlParameter("admin_user_id", adminUserId),
                new NpgsqlParameter("action", action),
                new NpgsqlParameter("target_user_id", NpgsqlDbType.Uuid) { Value = (object?)targetUserId ?? DBNull.Value },
                new NpgsqlParameter("target_group_id", NpgsqlDbType.Uuid) { Value = (object?)targetGroupId ?? DBNull.Value },
                new NpgsqlParameter("target_label", targetLabel),
                new NpgsqlParameter("reason", reason),
                new NpgsqlParameter("created_at", NpgsqlDbType.TimestampTz) { Value = (object?)createdAt ?? DBNull.Value });
        }

        public static Task InsertManyActionsAsync(MigratedDatabase database, Guid adminUserId, int count, DateTimeOffset firstCreatedAt)
        {
            return database.ExecuteAsync(
                "INSERT INTO admin_actions (admin_user_id, action, target_user_id, target_group_id, target_label, reason, created_at) "
                + "SELECT @admin_user_id, 'GroupClosed', NULL, gen_random_uuid(), 'Bulk group ' || number, 'Bulk reason ' || number, @first + make_interval(mins => number) "
                + "FROM generate_series(1, @count) AS number",
                new NpgsqlParameter("admin_user_id", adminUserId),
                new NpgsqlParameter("count", count),
                new NpgsqlParameter("first", NpgsqlDbType.TimestampTz) { Value = firstCreatedAt });
        }

        public static async Task<List<StoredAction>> ActionsAboutAsync(MigratedDatabase database, Guid targetId)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT row_to_json(a)::text FROM admin_actions a WHERE a.target_user_id = @id OR a.target_group_id = @id ORDER BY a.id",
                new NpgsqlParameter("id", targetId));

            return [.. rows.Select(row => StoredAction.Parse(row!))];
        }

        public static async Task<List<StoredAction>> ActionsByAsync(MigratedDatabase database, Guid adminUserId)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT row_to_json(a)::text FROM admin_actions a WHERE a.admin_user_id = @id ORDER BY a.id",
                new NpgsqlParameter("id", adminUserId));

            return [.. rows.Select(row => StoredAction.Parse(row!))];
        }

        public static async Task<int> CountActionsAsync(MigratedDatabase database)
        {
            List<string?> counts = await database.QueryAsync("SELECT count(*)::text FROM admin_actions");

            return int.Parse(Assert.Single(counts)!, CultureInfo.InvariantCulture);
        }
    }
}
