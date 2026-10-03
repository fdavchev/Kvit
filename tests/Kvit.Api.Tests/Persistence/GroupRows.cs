using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class GroupRows
    {
        public const string DefaultName = "Greece trip";
        public const string DefaultEmoji = "\U0001F3D6️";
        public const string DefaultCurrency = "MKD";
        public const string OwnerMemberName = "Ana";

        public static async Task<Guid> InsertGroupAsync(
            MigratedDatabase database,
            Guid ownerUserId,
            string kind = "Group",
            string name = DefaultName,
            string currency = DefaultCurrency,
            string status = "Open",
            string? inviteToken = null)
        {
            Guid groupId = Guid.CreateVersion7();

            await database.ExecuteAsync(
                "INSERT INTO groups (id, kind, name, emoji, default_currency, status, owner_user_id, invite_token, invite_token_created_at, created_by_user_id, created_at) "
                + "VALUES (@id, @kind, @name, @emoji, @currency, @status, @owner, @token, now(), @owner, now())",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("kind", kind),
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("emoji", DefaultEmoji),
                new NpgsqlParameter("currency", currency),
                new NpgsqlParameter("status", status),
                new NpgsqlParameter("owner", ownerUserId),
                new NpgsqlParameter("token", inviteToken ?? $"token-{groupId:N}"));

            return groupId;
        }

        public static async Task<Guid> InsertGroupWithOwnerAsync(
            MigratedDatabase database,
            Guid ownerUserId,
            string name = DefaultName,
            string currency = DefaultCurrency,
            string status = "Open")
        {
            Guid groupId = await InsertGroupAsync(database, ownerUserId, name: name, currency: currency, status: status);
            await InsertMemberAsync(database, groupId, ownerUserId, OwnerMemberName, ownerUserId);

            return groupId;
        }

        public static async Task<Guid> InsertGroupWithoutMembersAsync(MigratedDatabase database)
        {
            Guid ownerUserId = await UserRows.AddAsync(database);

            return await InsertGroupAsync(database, ownerUserId);
        }

        public static async Task<Guid> InsertMemberAsync(
            MigratedDatabase database,
            Guid groupId,
            Guid? userId,
            string name,
            Guid addedByUserId,
            DateTimeOffset? removedAt = null,
            string? endKind = null,
            DateTimeOffset? joinedAt = null,
            DateTimeOffset? claimedAt = null)
        {
            Guid memberId = Guid.CreateVersion7();
            object? removedBy = removedAt is null ? null : addedByUserId;

            await database.ExecuteAsync(
                "INSERT INTO group_members (id, group_id, user_id, name, joined_at, added_by_user_id, claimed_at, removed_at, removed_by_user_id, end_kind) "
                + "VALUES (@id, @group_id, @user_id, @name, COALESCE(@joined_at, now()), @added_by, @claimed_at, @removed_at, @removed_by, @end_kind)",
                new NpgsqlParameter("id", memberId),
                new NpgsqlParameter("group_id", groupId),
                Nullable("user_id", NpgsqlDbType.Uuid, userId),
                new NpgsqlParameter("name", name),
                Nullable("joined_at", NpgsqlDbType.TimestampTz, joinedAt),
                new NpgsqlParameter("added_by", addedByUserId),
                Nullable("claimed_at", NpgsqlDbType.TimestampTz, claimedAt),
                Nullable("removed_at", NpgsqlDbType.TimestampTz, removedAt),
                Nullable("removed_by", NpgsqlDbType.Uuid, removedBy),
                Nullable("end_kind", NpgsqlDbType.Text, endKind));

            return memberId;
        }

        public static Task InsertActivityEventAsync(
            MigratedDatabase database,
            Guid groupId,
            Guid actorUserId,
            string type,
            Guid? expenseId = null,
            Guid? settlementId = null)
        {
            return database.ExecuteAsync(
                "INSERT INTO activity_events (id, group_id, actor_user_id, type, expense_id, settlement_id, created_at) "
                + "VALUES (@id, @group_id, @actor, @type, @expense_id, @settlement_id, now())",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("actor", actorUserId),
                new NpgsqlParameter("type", type),
                Nullable("expense_id", NpgsqlDbType.Uuid, expenseId),
                Nullable("settlement_id", NpgsqlDbType.Uuid, settlementId));
        }

        public static Task SetDeletedAsync(MigratedDatabase database, Guid groupId, Guid deletedByUserId, int daysAgo)
        {
            return SetDeletedAgoAsync(database, groupId, deletedByUserId, TimeSpan.FromDays(daysAgo));
        }

        public static Task SetDeletedAgoAsync(MigratedDatabase database, Guid groupId, Guid deletedByUserId, TimeSpan deletedAgo)
        {
            return database.ExecuteAsync(
                "UPDATE groups SET deleted_at = now() - make_interval(secs => @seconds_ago), deleted_by_user_id = @deleted_by WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("seconds_ago", deletedAgo.TotalSeconds),
                new NpgsqlParameter("deleted_by", deletedByUserId));
        }

        public static Task SetStatusAsync(MigratedDatabase database, Guid groupId, string status)
        {
            return database.ExecuteAsync(
                "UPDATE groups SET status = @status WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("status", status));
        }

        public static Task EndMembershipAsync(MigratedDatabase database, Guid groupId, Guid userId, string endKind)
        {
            return database.ExecuteAsync(
                "UPDATE group_members SET removed_at = now(), removed_by_user_id = @user_id, end_kind = @end_kind WHERE group_id = @group_id AND user_id = @user_id",
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("end_kind", endKind));
        }

        public static async Task<string?> GroupValueAsync(MigratedDatabase database, Guid groupId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM groups WHERE id = @id",
                new NpgsqlParameter("id", groupId));

            return Assert.Single(values);
        }

        public static async Task<string?> MemberValueAsync(MigratedDatabase database, Guid groupId, Guid userId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM group_members WHERE group_id = @group_id AND user_id = @user_id",
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("user_id", userId));

            return Assert.Single(values);
        }

        public static async Task<string?> MemberRowValueAsync(MigratedDatabase database, Guid memberId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM group_members WHERE id = @id",
                new NpgsqlParameter("id", memberId));

            return Assert.Single(values);
        }

        public static async Task<Guid> CurrentMemberIdAsync(MigratedDatabase database, Guid groupId, Guid userId)
        {
            List<string?> ids = await database.QueryAsync(
                "SELECT id::text FROM group_members WHERE group_id = @group_id AND user_id = @user_id AND removed_at IS NULL",
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("user_id", userId));

            return Guid.Parse(Assert.Single(ids)!);
        }

        public static Task<int> CountMemberRowsOfUserAsync(MigratedDatabase database, Guid groupId, Guid userId)
        {
            return CountAsync(
                database,
                "SELECT count(*)::text FROM group_members WHERE group_id = @group_id AND user_id = @user_id",
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("user_id", userId));
        }

        public static async Task<string> SnapshotAsync(MigratedDatabase database, Guid groupId)
        {
            List<string?> parts = await database.QueryAsync(
                "SELECT part FROM ("
                + "SELECT 1 AS position, g::text AS part FROM groups g WHERE g.id = @group_id "
                + "UNION ALL SELECT 2, COALESCE(string_agg(m::text, '|' ORDER BY m.id), '') FROM group_members m WHERE m.group_id = @group_id "
                + "UNION ALL SELECT 3, count(*)::text FROM activity_events WHERE group_id = @group_id"
                + ") parts ORDER BY position",
                new NpgsqlParameter("group_id", groupId));

            return string.Join('#', parts);
        }

        public static Task SetPreviousInviteTokenAsync(MigratedDatabase database, Guid groupId, string previousToken)
        {
            return database.ExecuteAsync(
                "UPDATE groups SET previous_invite_token = @previous WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("previous", previousToken));
        }

        public static Task AgeInviteTokenAsync(MigratedDatabase database, Guid groupId)
        {
            return database.ExecuteAsync(
                "UPDATE groups SET invite_token_created_at = now() - interval '1 day' WHERE id = @id",
                new NpgsqlParameter("id", groupId));
        }

        public static async Task<string?> EventValueAsync(MigratedDatabase database, Guid groupId, string type, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM activity_events WHERE group_id = @group_id AND type = @type",
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("type", type));

            return Assert.Single(values);
        }

        public static Task<int> CountGroupsOwnedByAsync(MigratedDatabase database, Guid ownerUserId)
        {
            return CountAsync(
                database,
                "SELECT count(*)::text FROM groups WHERE owner_user_id = @owner",
                new NpgsqlParameter("owner", ownerUserId));
        }

        public static Task<int> CountMembersAsync(MigratedDatabase database, Guid groupId)
        {
            return CountAsync(
                database,
                "SELECT count(*)::text FROM group_members WHERE group_id = @group_id",
                new NpgsqlParameter("group_id", groupId));
        }

        public static Task<int> CountEventsAsync(MigratedDatabase database, Guid groupId)
        {
            return CountAsync(
                database,
                "SELECT count(*)::text FROM activity_events WHERE group_id = @group_id",
                new NpgsqlParameter("group_id", groupId));
        }

        public static Task<int> CountEventsOfTypeAsync(MigratedDatabase database, Guid groupId, string type)
        {
            return CountAsync(
                database,
                "SELECT count(*)::text FROM activity_events WHERE group_id = @group_id AND type = @type",
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("type", type));
        }

        public static Task<int> CountUsageEventsAsync(MigratedDatabase database, Guid userId, string type)
        {
            return CountAsync(
                database,
                "SELECT count(*)::text FROM usage_events WHERE user_id = @user_id AND type = @type",
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("type", type));
        }

        private static async Task<int> CountAsync(MigratedDatabase database, string sql, params NpgsqlParameter[] parameters)
        {
            List<string?> counts = await database.QueryAsync(sql, parameters);

            return int.Parse(Assert.Single(counts)!);
        }

        private static NpgsqlParameter Nullable(string name, NpgsqlDbType type, object? value)
        {
            return new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value };
        }
    }
}
