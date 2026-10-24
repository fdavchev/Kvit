using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.BudgetLimits
{
    public static class BudgetLimitSteps
    {
        public static readonly string[] LimitsFieldNames = ["categories", "month"];
        public static readonly string[] LimitRowFieldNames = ["categoryId", "includedEurMinor", "limitMinor", "overMinor", "spentMinor"];

        public static readonly Guid Food = PersonalEntryRequests.BuiltInFoodId;
        public static readonly Guid Transport = PersonalEntryRequests.BuiltInTransportId;

        public const string ClockMonthStart = "2026-10-01";

        private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss";

        public static async Task SetMonthAsync(SignedInUser user, long limitMinor)
        {
            HttpResponseMessage response = await BudgetLimitRequests.SetMonthAsync(user.Client, limitMinor);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public static async Task SetCategoryAsync(SignedInUser user, Guid categoryId, long limitMinor)
        {
            HttpResponseMessage response = await BudgetLimitRequests.SetCategoryAsync(user.Client, categoryId, limitMinor);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public static async Task<JsonElement> LimitsAsync(HttpClient client, string month = PersonalEntryRequests.DefaultMonth)
        {
            JsonElement body = await PersonalEntrySteps.MonthAsync(client, month);

            return body.GetProperty("limits");
        }

        public static JsonElement CategoryRowOf(JsonElement limits, Guid categoryId)
        {
            return Assert.Single(limits.GetProperty("categories").EnumerateArray(), row => row.GetProperty("categoryId").GetGuid() == categoryId);
        }

        public static void AssertRow(JsonElement row, Guid? categoryId, long limitMinor, long spentMinor, long overMinor, long includedEurMinor)
        {
            Assert.Equal(JsonValueKind.Object, row.ValueKind);
            Assert.Equal(LimitRowFieldNames, GroupRequests.PropertyNamesOf(row));
            JsonElement category = row.GetProperty("categoryId");
            if (categoryId is Guid expectedCategory)
            {
                Assert.Equal(expectedCategory, category.GetGuid());
            }
            else
            {
                Assert.Equal(JsonValueKind.Null, category.ValueKind);
            }

            Assert.Equal(limitMinor, row.GetProperty("limitMinor").GetInt64());
            Assert.Equal(spentMinor, row.GetProperty("spentMinor").GetInt64());
            Assert.Equal(overMinor, row.GetProperty("overMinor").GetInt64());
            Assert.Equal(includedEurMinor, row.GetProperty("includedEurMinor").GetInt64());
        }

        public static void AssertNoLimits(JsonElement limits)
        {
            Assert.Equal(LimitsFieldNames, GroupRequests.PropertyNamesOf(limits));
            Assert.Equal(JsonValueKind.Null, limits.GetProperty("month").ValueKind);
            Assert.Equal(0, limits.GetProperty("categories").GetArrayLength());
        }

        public static async Task<long?> StoredLimitAsync(MigratedDatabase database, Guid userId, Guid? categoryId, string effectiveFrom = ClockMonthStart)
        {
            List<string?> values = await database.QueryAsync(
                "SELECT coalesce(limit_minor::text, 'none') FROM budget_limits WHERE user_id = @user_id AND category_id IS NOT DISTINCT FROM @category_id AND effective_from = @effective_from",
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("category_id", NpgsqlDbType.Uuid) { Value = (object?)categoryId ?? DBNull.Value },
                new NpgsqlParameter("effective_from", NpgsqlDbType.Date) { Value = DateOnly.Parse(effectiveFrom, CultureInfo.InvariantCulture) });
            if (values.Count == 0 || values[0] == "none")
            {
                return null;
            }

            return long.Parse(Assert.Single(values)!, CultureInfo.InvariantCulture);
        }

        public static async Task<List<string>> RowsAsync(MigratedDatabase database, Guid userId, Guid? categoryId)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT effective_from::text || ':' || coalesce(limit_minor::text, 'none') FROM budget_limits WHERE user_id = @user_id AND category_id IS NOT DISTINCT FROM @category_id ORDER BY effective_from",
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("category_id", NpgsqlDbType.Uuid) { Value = (object?)categoryId ?? DBNull.Value });

            return [.. rows.Select(row => row!)];
        }

        public static async Task<int> CountOfUserAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> counts = await database.QueryAsync(
                "SELECT count(*)::text FROM budget_limits WHERE user_id = @user_id",
                new NpgsqlParameter("user_id", userId));

            return int.Parse(Assert.Single(counts)!);
        }

        public static async Task<string> SnapshotAsync(MigratedDatabase database)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT concat_ws('|', id, user_id, category_id, limit_minor, created_at, updated_at) FROM budget_limits ORDER BY id");

            return string.Join("\n", rows);
        }

        public static async Task<string?> TimestampAsync(MigratedDatabase database, Guid userId, Guid? categoryId, string column)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT to_char({column} AT TIME ZONE 'UTC', 'YYYY-MM-DD\"T\"HH24:MI:SS') FROM budget_limits WHERE user_id = @user_id AND category_id IS NOT DISTINCT FROM @category_id",
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("category_id", NpgsqlDbType.Uuid) { Value = (object?)categoryId ?? DBNull.Value });

            return Assert.Single(values);
        }

        public static string TextOf(DateTimeOffset moment)
        {
            return moment.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture);
        }

        public static async Task<Guid> AddSpendingAsync(ExpensesApp app, SignedInUser user, Guid categoryId, long amountMinor, string currency, decimal mkdPerEur, string entryDate = PersonalEntryRequests.DefaultDate)
        {
            await BudgetCurrencySteps.UseRateAsync(app, mkdPerEur);

            return await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(categoryId, amountMinor, currency, entryDate));
        }

        public static async Task<Guid> AddSharedExpenseAsync(ExpensesApp app, ExpenseGroup group, Guid? categoryId, long amountMinor, string currency, decimal mkdPerEur, string expenseDate = ExpenseInputs.DefaultDate)
        {
            await BudgetCurrencySteps.UseRateAsync(app, mkdPerEur);
            ExpenseInput input = await BudgetShareSteps.SharedInputAsync(app, group, expenseDate, amountMinor, currency) with { CategoryId = categoryId };

            return await app.AddExpenseAsync(group.Owner, group.GroupId, input);
        }

        public static async Task AddMixedMonthAsync(ExpensesApp app, ExpenseGroup group)
        {
            SignedInUser me = group.Member;
            await AddSpendingAsync(app, me, Food, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await AddSpendingAsync(app, me, Food, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            await AddSpendingAsync(app, me, Food, 1_234, BudgetCurrencyRequests.Eur, 61.5000m);
            await AddSharedExpenseAsync(app, group, Food, 100_000, BudgetCurrencyRequests.Mkd, 61.4000m);
            await AddSharedExpenseAsync(app, group, Food, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await AddSpendingAsync(app, me, Transport, 30_000, BudgetCurrencyRequests.Mkd, 62.0000m);
            await AddSpendingAsync(app, me, Transport, 1_550, BudgetCurrencyRequests.Eur, 62.0000m);
            await AddSharedExpenseAsync(app, group, Transport, 2_000, BudgetCurrencyRequests.Eur, 61.0000m);
            await BudgetCurrencySteps.AddIncomeAsync(app, me, 4_500_000, BudgetCurrencyRequests.Mkd, 61.2500m);
            await BudgetCurrencySteps.AddIncomeAsync(app, me, 10_000, BudgetCurrencyRequests.Eur, 61.8123m);
        }
    }
}
