using System.Globalization;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class ExpenseRows
    {
        public const long DefaultAmountMinor = 100_000;
        public const decimal DefaultMkdPerEur = 61.5610m;
        public static readonly DateOnly DefaultRateDate = new(2026, 9, 24);
        public static readonly DateOnly DefaultExpenseDate = new(2026, 10, 5);

        public static async Task<Guid> InsertExpenseAsync(
            MigratedDatabase database,
            Guid groupId,
            Guid paidByMemberId,
            Guid createdByUserId,
            long amountMinor = DefaultAmountMinor,
            string currency = "MKD",
            string splitType = "Equal",
            string? title = null,
            string? note = null,
            Guid? categoryId = null,
            Guid? clientRequestId = null,
            DateTimeOffset? deletedAt = null)
        {
            Guid expenseId = Guid.CreateVersion7();
            object? deletedBy = deletedAt is null ? null : createdByUserId;

            await database.ExecuteAsync(
                "INSERT INTO expenses (id, group_id, title, note, amount_minor, currency, expense_date, category_id, paid_by_member_id, split_type, "
                + "mkd_per_eur, rate_date, created_by_user_id, created_at, deleted_at, deleted_by_user_id, client_request_id) "
                + "VALUES (@id, @group_id, @title, @note, @amount_minor, @currency, @expense_date, @category_id, @paid_by, @split_type, "
                + "@mkd_per_eur, @rate_date, @created_by, now(), @deleted_at, @deleted_by, @client_request_id)",
                new NpgsqlParameter("id", expenseId),
                new NpgsqlParameter("group_id", groupId),
                Nullable("title", NpgsqlDbType.Text, title),
                Nullable("note", NpgsqlDbType.Text, note),
                new NpgsqlParameter("amount_minor", amountMinor),
                new NpgsqlParameter("currency", currency),
                new NpgsqlParameter("expense_date", DefaultExpenseDate),
                Nullable("category_id", NpgsqlDbType.Uuid, categoryId),
                new NpgsqlParameter("paid_by", paidByMemberId),
                new NpgsqlParameter("split_type", splitType),
                new NpgsqlParameter("mkd_per_eur", DefaultMkdPerEur),
                new NpgsqlParameter("rate_date", DefaultRateDate),
                new NpgsqlParameter("created_by", createdByUserId),
                Nullable("deleted_at", NpgsqlDbType.TimestampTz, deletedAt),
                Nullable("deleted_by", NpgsqlDbType.Uuid, deletedBy),
                new NpgsqlParameter("client_request_id", clientRequestId ?? Guid.CreateVersion7()));

            return expenseId;
        }

        public static Task InsertShareAsync(MigratedDatabase database, Guid expenseId, Guid memberId, long inputValue = 0, long shareMinor = 0)
        {
            return database.ExecuteAsync(
                "INSERT INTO expense_shares (expense_id, member_id, input_value, share_minor) VALUES (@expense_id, @member_id, @input_value, @share_minor)",
                new NpgsqlParameter("expense_id", expenseId),
                new NpgsqlParameter("member_id", memberId),
                new NpgsqlParameter("input_value", inputValue),
                new NpgsqlParameter("share_minor", shareMinor));
        }

        public static async Task<string?> ExpenseValueAsync(MigratedDatabase database, Guid expenseId, string expression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({expression})::text FROM expenses WHERE id = @id",
                new NpgsqlParameter("id", expenseId));

            return Assert.Single(values);
        }

        public static async Task<List<StoredShare>> SharesOfAsync(MigratedDatabase database, Guid expenseId)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT s.member_id::text || ':' || s.input_value::text || ':' || s.share_minor::text "
                + "FROM expense_shares s JOIN group_members m ON m.id = s.member_id "
                + "WHERE s.expense_id = @expense_id ORDER BY m.joined_at, m.id",
                new NpgsqlParameter("expense_id", expenseId));

            return [.. rows.Select(ParseShare)];
        }

        public static Task<int> CountExpensesAsync(MigratedDatabase database, Guid groupId)
        {
            return CountAsync(
                database,
                "SELECT count(*)::text FROM expenses WHERE group_id = @group_id",
                new NpgsqlParameter("group_id", groupId));
        }

        public static Task<int> CountExpensesCreatedByAsync(MigratedDatabase database, Guid userId)
        {
            return CountAsync(
                database,
                "SELECT count(*)::text FROM expenses WHERE created_by_user_id = @user_id",
                new NpgsqlParameter("user_id", userId));
        }

        public static Task<int> CountEventsByActorAsync(MigratedDatabase database, Guid userId)
        {
            return CountAsync(
                database,
                "SELECT count(*)::text FROM activity_events WHERE actor_user_id = @user_id",
                new NpgsqlParameter("user_id", userId));
        }

        public static Task<int> CountSharesOfAsync(MigratedDatabase database, Guid expenseId)
        {
            return CountAsync(
                database,
                "SELECT count(*)::text FROM expense_shares WHERE expense_id = @expense_id",
                new NpgsqlParameter("expense_id", expenseId));
        }

        public static async Task<string> SnapshotAsync(MigratedDatabase database, Guid groupId)
        {
            List<string?> parts = await database.QueryAsync(
                "SELECT part FROM ("
                + "SELECT 1 AS position, COALESCE(string_agg(e::text, '|' ORDER BY e.id), '') AS part FROM expenses e WHERE e.group_id = @group_id "
                + "UNION ALL SELECT 2, COALESCE(string_agg(s::text, '|' ORDER BY s.expense_id, s.member_id), '') FROM expense_shares s JOIN expenses e ON e.id = s.expense_id WHERE e.group_id = @group_id "
                + "UNION ALL SELECT 3, COALESCE(string_agg(a::text, '|' ORDER BY a.id), '') FROM activity_events a WHERE a.group_id = @group_id"
                + ") parts ORDER BY position",
                new NpgsqlParameter("group_id", groupId));

            return string.Join('#', parts);
        }

        public static Task EndMemberRowAsync(MigratedDatabase database, Guid memberRowId, Guid removedByUserId)
        {
            return database.ExecuteAsync(
                "UPDATE group_members SET removed_at = now(), removed_by_user_id = @removed_by, end_kind = 'Removed' WHERE id = @id",
                new NpgsqlParameter("id", memberRowId),
                new NpgsqlParameter("removed_by", removedByUserId));
        }

        public static Task BlockAllShareChangesAsync(MigratedDatabase database)
        {
            return database.ExecuteAsync(
                "CREATE OR REPLACE FUNCTION block_share_changes() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'expense_shares are blocked by the test'; END $$; "
                + "DROP TRIGGER IF EXISTS block_share_changes ON expense_shares; "
                + "CREATE TRIGGER block_share_changes BEFORE INSERT OR UPDATE OR DELETE ON expense_shares FOR EACH ROW EXECUTE FUNCTION block_share_changes();");
        }

        public static Task AllowAllShareChangesAsync(MigratedDatabase database)
        {
            return database.ExecuteAsync("DROP TRIGGER IF EXISTS block_share_changes ON expense_shares");
        }

        private static StoredShare ParseShare(string? row)
        {
            string[] parts = (row ?? throw new InvalidOperationException("A share row came back as null.")).Split(':');

            return new StoredShare(
                Guid.Parse(parts[0]),
                long.Parse(parts[1], CultureInfo.InvariantCulture),
                long.Parse(parts[2], CultureInfo.InvariantCulture));
        }

        private static async Task<int> CountAsync(MigratedDatabase database, string sql, params NpgsqlParameter[] parameters)
        {
            List<string?> counts = await database.QueryAsync(sql, parameters);

            return int.Parse(Assert.Single(counts)!, CultureInfo.InvariantCulture);
        }

        private static NpgsqlParameter Nullable(string name, NpgsqlDbType type, object? value)
        {
            return new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value };
        }
    }
}
