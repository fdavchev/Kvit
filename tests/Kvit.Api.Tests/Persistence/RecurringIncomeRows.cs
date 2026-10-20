using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class RecurringIncomeRows
    {
        public static async Task<Guid> InsertAsync(
            MigratedDatabase database,
            Guid userId,
            string frequency = "Monthly",
            int? dayOfMonth = 15,
            int? dayOfWeek = null,
            long amountMinor = 4_500_000,
            string currency = "MKD",
            string? title = "Salary",
            DateOnly? startsOn = null,
            DateOnly? endsOn = null,
            DateTimeOffset? stoppedAt = null,
            Guid? clientRequestId = null)
        {
            Guid recurringIncomeId = Guid.CreateVersion7();
            await database.ExecuteAsync(
                "INSERT INTO recurring_incomes (id, user_id, amount_minor, currency, title, frequency, day_of_month, day_of_week, starts_on, ends_on, client_request_id, created_at, stopped_at) "
                + "VALUES (@id, @user_id, @amount_minor, @currency, @title, @frequency, @day_of_month, @day_of_week, @starts_on, @ends_on, @client_request_id, now(), @stopped_at)",
                new NpgsqlParameter("id", recurringIncomeId),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("amount_minor", amountMinor),
                new NpgsqlParameter("currency", currency),
                new NpgsqlParameter("title", NpgsqlDbType.Text) { Value = (object?)title ?? DBNull.Value },
                new NpgsqlParameter("frequency", frequency),
                new NpgsqlParameter("day_of_month", NpgsqlDbType.Integer) { Value = (object?)dayOfMonth ?? DBNull.Value },
                new NpgsqlParameter("day_of_week", NpgsqlDbType.Integer) { Value = (object?)dayOfWeek ?? DBNull.Value },
                new NpgsqlParameter("starts_on", NpgsqlDbType.Date) { Value = startsOn ?? new DateOnly(2026, 1, 1) },
                new NpgsqlParameter("ends_on", NpgsqlDbType.Date) { Value = (object?)endsOn ?? DBNull.Value },
                new NpgsqlParameter("client_request_id", clientRequestId ?? Guid.CreateVersion7()),
                new NpgsqlParameter("stopped_at", NpgsqlDbType.TimestampTz) { Value = (object?)stoppedAt ?? DBNull.Value });

            return recurringIncomeId;
        }

        public static Task InsertEntryAsync(MigratedDatabase database, Guid userId, Guid recurringIncomeId, DateOnly entryDate, DateTimeOffset? deletedAt = null)
        {
            return database.ExecuteAsync(
                "INSERT INTO personal_entries (id, user_id, kind, amount_minor, currency, mkd_per_eur, rate_date, entry_date, category_id, note, recurring_income_id, client_request_id, created_at, deleted_at) "
                + "VALUES (@id, @user_id, 'Income', 4500000, 'MKD', 61.5, DATE '2026-10-05', @entry_date, NULL, NULL, @recurring_income_id, @client_request_id, now(), @deleted_at)",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("entry_date", NpgsqlDbType.Date) { Value = entryDate },
                new NpgsqlParameter("recurring_income_id", recurringIncomeId),
                new NpgsqlParameter("client_request_id", Guid.CreateVersion7()),
                new NpgsqlParameter("deleted_at", NpgsqlDbType.TimestampTz) { Value = (object?)deletedAt ?? DBNull.Value });
        }

        public static async Task<int> CountOfUserAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> counts = await database.QueryAsync(
                "SELECT count(*)::text FROM recurring_incomes WHERE user_id = @user_id",
                new NpgsqlParameter("user_id", userId));

            return int.Parse(Assert.Single(counts)!);
        }

        public static async Task<string?> ValueAsync(MigratedDatabase database, Guid recurringIncomeId, string columnExpression)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT ({columnExpression})::text FROM recurring_incomes WHERE id = @id",
                new NpgsqlParameter("id", recurringIncomeId));

            return Assert.Single(values);
        }

        public static async Task<string> SnapshotAsync(MigratedDatabase database)
        {
            List<string?> rows = await database.QueryAsync("SELECT r::text FROM recurring_incomes r ORDER BY r.id");

            return string.Join("\n", rows);
        }

        public static async Task<string> EntriesSnapshotAsync(MigratedDatabase database)
        {
            List<string?> rows = await database.QueryAsync("SELECT e::text FROM personal_entries e ORDER BY e.id");

            return string.Join("\n", rows);
        }

        public static Task SetStoppedAtAsync(MigratedDatabase database, Guid recurringIncomeId, DateTimeOffset stoppedAt)
        {
            return database.ExecuteAsync(
                "UPDATE recurring_incomes SET stopped_at = @stopped_at WHERE id = @id",
                new NpgsqlParameter("stopped_at", NpgsqlDbType.TimestampTz) { Value = stoppedAt },
                new NpgsqlParameter("id", recurringIncomeId));
        }

        public static Task SetStartsOnAsync(MigratedDatabase database, Guid recurringIncomeId, DateOnly startsOn)
        {
            return database.ExecuteAsync(
                "UPDATE recurring_incomes SET starts_on = @starts_on WHERE id = @id",
                new NpgsqlParameter("starts_on", NpgsqlDbType.Date) { Value = startsOn },
                new NpgsqlParameter("id", recurringIncomeId));
        }
    }
}
