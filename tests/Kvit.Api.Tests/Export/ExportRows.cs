using System.Globalization;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public static class ExportRows
    {
        private static readonly string[] TablesAnExportMustNotChange =
        [
            "users",
            "user_logins",
            "user_roles",
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

        public static async Task<string> SnapshotOfEveryTableAsync(MigratedDatabase database)
        {
            List<string> parts = [];
            foreach (string table in TablesAnExportMustNotChange)
            {
                List<string?> rows = await database.QueryAsync(
                    $"SELECT count(*)::text || ':' || coalesce(md5(string_agg(t::text, '|' ORDER BY t::text)), '') FROM {table} t");

                parts.Add($"{table}={Assert.Single(rows)}");
            }

            return string.Join('\n', parts);
        }

        public static async Task<int> CountUsageEventsAsync(MigratedDatabase database)
        {
            List<string?> counts = await database.QueryAsync("SELECT count(*)::text FROM usage_events");

            return int.Parse(Assert.Single(counts)!);
        }

        public static async Task<string> UserValueAsync(MigratedDatabase database, Guid userId, string column)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT {column} FROM users WHERE id = @id",
                new NpgsqlParameter("id", userId));

            return Assert.Single(values) ?? throw new InvalidOperationException($"users.{column} is null for {userId}.");
        }

        public static async Task<DateTimeOffset> MemberJoinedAtAsync(MigratedDatabase database, Guid memberRowId)
        {
            string? text = await GroupRows.MemberRowValueAsync(database, memberRowId, "to_char(joined_at AT TIME ZONE 'UTC', 'YYYY-MM-DD\"T\"HH24:MI:SS.US\"Z\"')");

            return DateTimeOffset.Parse(
                text ?? throw new InvalidOperationException($"group_members.joined_at is null for {memberRowId}."),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal);
        }

        public static Task EndMemberRowAtAsync(MigratedDatabase database, Guid memberRowId, Guid removedByUserId, DateTimeOffset removedAt, string endKind)
        {
            return database.ExecuteAsync(
                "UPDATE group_members SET removed_at = @removed_at, removed_by_user_id = @removed_by, end_kind = @end_kind WHERE id = @id",
                new NpgsqlParameter("id", memberRowId),
                new NpgsqlParameter("removed_at", removedAt),
                new NpgsqlParameter("removed_by", removedByUserId),
                new NpgsqlParameter("end_kind", endKind));
        }

        public static Task SetCountsInBudgetAsync(MigratedDatabase database, Guid memberRowId, bool countsInBudget)
        {
            return database.ExecuteAsync(
                "UPDATE group_members SET counts_in_budget = @counts WHERE id = @id",
                new NpgsqlParameter("id", memberRowId),
                new NpgsqlParameter("counts", countsInBudget));
        }

        public static Task StartClosingAsync(MigratedDatabase database, Guid groupId, DateTimeOffset startedAt)
        {
            return database.ExecuteAsync(
                "UPDATE groups SET status = 'Closing', closing_started_at = @started WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("started", startedAt));
        }

        public static async Task<string> RecurringIncomeEntryIdAsync(MigratedDatabase database, Guid recurringIncomeId)
        {
            List<string?> ids = await database.QueryAsync(
                "SELECT id::text FROM personal_entries WHERE recurring_income_id = @id",
                new NpgsqlParameter("id", recurringIncomeId));

            return Assert.Single(ids)!;
        }
    }
}
