using System.Globalization;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public static class DeletionRows
    {
        private static readonly string[] TablesExceptUsers =
        [
            "user_logins",
            "user_roles",
            "user_claims",
            "user_tokens",
            "groups",
            "group_members",
            "expenses",
            "expense_shares",
            "settlements",
            "closing_responses",
            "activity_events",
            "usage_events",
            "personal_entries",
            "categories",
            "budget_limits",
            "recurring_incomes",
            "duplicate_answers",
            "exchange_rates",
        ];

        private static readonly string[] TablesAPurgeMustNotTouch =
        [
            "expenses",
            "expense_shares",
            "settlements",
            "closing_responses",
            "exchange_rates",
        ];

        public static async Task<StoredDeletion> ReadAsync(MigratedDatabase database, Guid userId)
        {
            DateTimeOffset? requestedAt = await TimeOfAsync(database, "users WHERE id = @id", "deletion_requested_at", new NpgsqlParameter("id", userId));
            string? nameChoice = await UserValueAsync(database, userId, "deletion_name_choice");
            DateTimeOffset? purgedAt = await TimeOfAsync(database, "users WHERE id = @id", "purged_at", new NpgsqlParameter("id", userId));

            return new StoredDeletion(requestedAt, nameChoice, purgedAt);
        }

        public static Task SetAsync(MigratedDatabase database, Guid userId, DateTimeOffset? requestedAt, string? nameChoice, DateTimeOffset? purgedAt)
        {
            return database.ExecuteAsync(
                "UPDATE users SET deletion_requested_at = @requested_at, deletion_name_choice = @name_choice, purged_at = @purged_at WHERE id = @id",
                new NpgsqlParameter("id", userId),
                new NpgsqlParameter("requested_at", NpgsqlDbType.TimestampTz) { Value = (object?)requestedAt ?? DBNull.Value },
                new NpgsqlParameter("name_choice", NpgsqlDbType.Text) { Value = (object?)nameChoice ?? DBNull.Value },
                new NpgsqlParameter("purged_at", NpgsqlDbType.TimestampTz) { Value = (object?)purgedAt ?? DBNull.Value });
        }

        public static async Task<string?> UserValueAsync(MigratedDatabase database, Guid userId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM users WHERE id = @id",
                new NpgsqlParameter("id", userId));

            return Assert.Single(values);
        }

        public static async Task<DateTimeOffset?> TimeOfAsync(MigratedDatabase database, string fromAndWhere, string column, params NpgsqlParameter[] parameters)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT to_char({column} AT TIME ZONE 'UTC', 'YYYY-MM-DD\"T\"HH24:MI:SS.US\"Z\"') FROM {fromAndWhere}",
                parameters);
            string? text = Assert.Single(values);

            return text is null ? null : DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        }

        public static async Task<string> SnapshotOfUserRowAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT t::text FROM users t WHERE t.id = @id",
                new NpgsqlParameter("id", userId));

            return Assert.Single(rows) ?? throw new InvalidOperationException($"The users row of {userId} came back as null.");
        }

        public static Task<string> SnapshotOfEveryTableExceptUsersAsync(MigratedDatabase database)
        {
            return SnapshotOfTablesAsync(database, TablesExceptUsers);
        }

        public static Task<string> SnapshotOfTheTablesAPurgeMustNotTouchAsync(MigratedDatabase database)
        {
            return SnapshotOfTablesAsync(database, TablesAPurgeMustNotTouch);
        }

        public static async Task<string> SnapshotOfTablesAsync(MigratedDatabase database, params string[] tables)
        {
            List<string> parts = [];
            foreach (string table in tables)
            {
                List<string?> rows = await database.QueryAsync(
                    $"SELECT count(*)::text || ':' || coalesce(md5(string_agg(t::text, '|' ORDER BY t::text)), '') FROM {table} t");

                parts.Add($"{table}={Assert.Single(rows)}");
            }

            return string.Join('\n', parts);
        }

        public static async Task<string> SnapshotOfRowsOfUserAsync(MigratedDatabase database, Guid userId)
        {
            (string Table, string Column)[] tables =
            [
                ("users", "id"),
                ("user_logins", "user_id"),
                ("user_roles", "user_id"),
                ("personal_entries", "user_id"),
                ("recurring_incomes", "user_id"),
                ("budget_limits", "user_id"),
                ("categories", "owner_user_id"),
                ("duplicate_answers", "user_id"),
                ("usage_events", "user_id"),
                ("group_members", "user_id"),
            ];
            List<string> parts = [];
            foreach ((string table, string column) in tables)
            {
                List<string?> rows = await database.QueryAsync(
                    $"SELECT coalesce(string_agg(t::text, '|' ORDER BY t::text), '') FROM {table} t WHERE t.{column} = @id",
                    new NpgsqlParameter("id", userId));

                parts.Add($"{table}={Assert.Single(rows)}");
            }

            return string.Join('\n', parts);
        }

        public static async Task<PersonalCounts> PersonalCountsAsync(MigratedDatabase database, Guid userId)
        {
            return new PersonalCounts(
                await CountOfUserAsync(database, "personal_entries", "user_id", userId),
                await CountOfUserAsync(database, "recurring_incomes", "user_id", userId),
                await CountOfUserAsync(database, "budget_limits", "user_id", userId),
                await CountOfUserAsync(database, "categories", "owner_user_id", userId),
                await CountOfUserAsync(database, "duplicate_answers", "user_id", userId));
        }

        public static Task<int> CountOfUserAsync(MigratedDatabase database, string table, string column, Guid userId)
        {
            return CountAsync(database, $"SELECT count(*)::text FROM {table} WHERE {column} = @id", new NpgsqlParameter("id", userId));
        }

        public static Task<int> CountUsersWithIdAsync(MigratedDatabase database, Guid userId)
        {
            return CountAsync(database, "SELECT count(*)::text FROM users WHERE id = @id", new NpgsqlParameter("id", userId));
        }

        public static Task SetJoinedAtAsync(MigratedDatabase database, Guid memberRowId, DateTimeOffset joinedAt)
        {
            return database.ExecuteAsync(
                "UPDATE group_members SET joined_at = @joined_at WHERE id = @id",
                new NpgsqlParameter("id", memberRowId),
                new NpgsqlParameter("joined_at", NpgsqlDbType.TimestampTz) { Value = joinedAt });
        }

        public static async Task<Guid> OwnerOfAsync(MigratedDatabase database, Guid groupId)
        {
            string owner = await GroupRows.GroupValueAsync(database, groupId, "owner_user_id")
                ?? throw new InvalidOperationException($"Group {groupId} has no owner.");

            return Guid.Parse(owner);
        }

        public static async Task<Guid> InsertEventAsync(
            MigratedDatabase database,
            Guid groupId,
            Guid actorUserId,
            string type,
            Guid? memberId,
            string? dataJson,
            string? changesJson)
        {
            Guid eventId = Guid.CreateVersion7();

            await database.ExecuteAsync(
                "INSERT INTO activity_events (id, group_id, actor_user_id, type, member_id, data, changes, created_at) "
                + "VALUES (@id, @group_id, @actor, @type, @member_id, @data, @changes, now())",
                new NpgsqlParameter("id", eventId),
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("actor", actorUserId),
                new NpgsqlParameter("type", type),
                new NpgsqlParameter("member_id", NpgsqlDbType.Uuid) { Value = (object?)memberId ?? DBNull.Value },
                new NpgsqlParameter("data", NpgsqlDbType.Jsonb) { Value = (object?)dataJson ?? DBNull.Value },
                new NpgsqlParameter("changes", NpgsqlDbType.Jsonb) { Value = (object?)changesJson ?? DBNull.Value });

            return eventId;
        }

        public static async Task<string?> EventValueAsync(MigratedDatabase database, Guid eventId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM activity_events WHERE id = @id",
                new NpgsqlParameter("id", eventId));

            return Assert.Single(values);
        }

        public static async Task<List<string>> NamesOfCurrentRowsAsync(MigratedDatabase database, Guid groupId)
        {
            List<string?> names = await database.QueryAsync(
                "SELECT name FROM group_members WHERE group_id = @group_id AND removed_at IS NULL ORDER BY joined_at, id",
                new NpgsqlParameter("group_id", groupId));

            return [.. names.Select(name => name!)];
        }

        public static async Task AddClaimAndTokenAsync(MigratedDatabase database, Guid userId)
        {
            await database.ExecuteAsync(
                "INSERT INTO user_claims (user_id, claim_type, claim_value) VALUES (@id, 'test-claim', 'test-value')",
                new NpgsqlParameter("id", userId));
            await database.ExecuteAsync(
                "INSERT INTO user_tokens (user_id, login_provider, name, value) VALUES (@id, 'test-provider', 'test-token', 'test-value')",
                new NpgsqlParameter("id", userId));
        }

        private static async Task<int> CountAsync(MigratedDatabase database, string sql, params NpgsqlParameter[] parameters)
        {
            List<string?> counts = await database.QueryAsync(sql, parameters);

            return int.Parse(Assert.Single(counts)!, CultureInfo.InvariantCulture);
        }
    }
}
