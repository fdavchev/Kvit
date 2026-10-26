using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.BudgetLimits;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.BudgetSummary
{
    public static class BudgetSummarySteps
    {
        public const string ThisMonth = PersonalEntryRequests.DefaultMonth;
        public const string PreviousMonth = "2026-09";

        public const long MkdSpendingThis = 170_000;
        public const long MkdSpendingPrevious = 100_000;
        public const long MkdIncomeThis = 4_500_000;
        public const long MkdIncomePrevious = 4_000_000;
        public const long EurSpendingThis = 4_050;
        public const long EurSpendingPrevious = 2_234;
        public const long EurIncomeThis = 10_000;
        public const long EurIncomePrevious = 0;

        public const long ConvertedToMkdSpendingThis = 420_100;
        public const long ConvertedToMkdSpendingPrevious = 237_500;
        public const long ConvertedToMkdIncomeThis = 5_118_100;
        public const long ConvertedToMkdIncomePrevious = 4_000_000;
        public const long ConvertedToEurSpendingThis = 6_831;
        public const long ConvertedToEurSpendingPrevious = 3_866;
        public const long ConvertedToEurIncomeThis = 83_469;
        public const long ConvertedToEurIncomePrevious = 65_574;

        public static readonly string[] SummaryFieldNames = ["convertedTotals", "currencies", "month", "previousMonth"];
        public static readonly string[] CurrencyFieldNames = ["categories", "currency", "incomePreviousMinor", "incomeThisMinor", "spendingPreviousMinor", "spendingThisMinor"];
        public static readonly string[] CategoryFieldNames = ["categoryId", "previousMinor", "thisMinor"];
        public static readonly string[] ConvertedTotalsFieldNames = ["currency", "incomePreviousMinor", "incomeThisMinor", "spendingPreviousMinor", "spendingThisMinor"];

        public static async Task<JsonElement> SummaryAsync(HttpClient client, string month = ThisMonth)
        {
            HttpResponseMessage response = await BudgetSummaryRequests.GetAsync(client, month);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        public static async Task<JsonElement> ConvertedTotalsAsync(HttpClient client, string month = ThisMonth)
        {
            JsonElement summary = await SummaryAsync(client, month);

            return summary.GetProperty("convertedTotals");
        }

        public static List<string?> CurrencyNamesOf(JsonElement summary)
        {
            return [.. summary.GetProperty("currencies").EnumerateArray().Select(row => row.GetProperty("currency").GetString())];
        }

        public static JsonElement CurrencyRowOf(JsonElement summary, string currency)
        {
            return Assert.Single(summary.GetProperty("currencies").EnumerateArray(), row => row.GetProperty("currency").GetString() == currency);
        }

        public static void AssertCurrencyRow(JsonElement summary, string currency, long spendingThis, long spendingPrevious, long incomeThis, long incomePrevious)
        {
            JsonElement row = CurrencyRowOf(summary, currency);

            Assert.Equal(CurrencyFieldNames, GroupRequests.PropertyNamesOf(row));
            Assert.Equal(spendingThis, row.GetProperty("spendingThisMinor").GetInt64());
            Assert.Equal(spendingPrevious, row.GetProperty("spendingPreviousMinor").GetInt64());
            Assert.Equal(incomeThis, row.GetProperty("incomeThisMinor").GetInt64());
            Assert.Equal(incomePrevious, row.GetProperty("incomePreviousMinor").GetInt64());
        }

        public static void AssertCategories(JsonElement summary, string currency, params (Guid CategoryId, long ThisMinor, long PreviousMinor)[] expected)
        {
            JsonElement[] rows = [.. CurrencyRowOf(summary, currency).GetProperty("categories").EnumerateArray()];

            foreach (JsonElement row in rows)
            {
                Assert.Equal(CategoryFieldNames, GroupRequests.PropertyNamesOf(row));
            }

            List<(Guid CategoryId, long ThisMinor, long PreviousMinor)> actual = [.. rows.Select(row => (row.GetProperty("categoryId").GetGuid(), row.GetProperty("thisMinor").GetInt64(), row.GetProperty("previousMinor").GetInt64()))];
            List<(Guid CategoryId, long ThisMinor, long PreviousMinor)> expectedRows = [.. expected];
            Assert.Equal(expectedRows, actual);
        }

        public static void AssertConvertedTotals(JsonElement convertedTotals, string currency, long spendingThis, long spendingPrevious, long incomeThis, long incomePrevious)
        {
            Assert.Equal(JsonValueKind.Object, convertedTotals.ValueKind);
            Assert.Equal(ConvertedTotalsFieldNames, GroupRequests.PropertyNamesOf(convertedTotals));
            Assert.Equal(currency, convertedTotals.GetProperty("currency").GetString());
            Assert.Equal(spendingThis, convertedTotals.GetProperty("spendingThisMinor").GetInt64());
            Assert.Equal(spendingPrevious, convertedTotals.GetProperty("spendingPreviousMinor").GetInt64());
            Assert.Equal(incomeThis, convertedTotals.GetProperty("incomeThisMinor").GetInt64());
            Assert.Equal(incomePrevious, convertedTotals.GetProperty("incomePreviousMinor").GetInt64());
        }

        public static void AssertNothingToCompare(JsonElement summary)
        {
            Assert.Equal(0, summary.GetProperty("currencies").GetArrayLength());
        }

        public static async Task AddTwoMonthsAsync(ExpensesApp app, ExpenseGroup group)
        {
            SignedInUser me = group.Member;
            Guid food = BudgetLimitSteps.Food;
            Guid transport = BudgetLimitSteps.Transport;
            string mkd = BudgetCurrencyRequests.Mkd;
            string eur = BudgetCurrencyRequests.Eur;

            await BudgetLimitSteps.AddSpendingAsync(app, me, food, 100_000, mkd, 61.0000m);
            await BudgetLimitSteps.AddSpendingAsync(app, me, food, 2_000, eur, 61.5610m);
            await BudgetLimitSteps.AddSpendingAsync(app, me, transport, 1_550, eur, 62.0000m);
            await BudgetLimitSteps.AddSharedExpenseAsync(app, group, food, 100_000, mkd, 61.4000m);
            await BudgetLimitSteps.AddSharedExpenseAsync(app, group, food, 1_000, eur, 61.7000m);
            await BudgetLimitSteps.AddSharedExpenseAsync(app, group, null, 40_000, mkd, 61.0000m);
            await BudgetCurrencySteps.AddIncomeAsync(app, me, 4_500_000, mkd, 61.2500m);
            await BudgetCurrencySteps.AddIncomeAsync(app, me, 10_000, eur, 61.8123m);

            await BudgetLimitSteps.AddSpendingAsync(app, me, food, 60_000, mkd, 61.0000m, "2026-09-10");
            await BudgetLimitSteps.AddSpendingAsync(app, me, food, 1_234, eur, 62.0000m, "2026-09-11");
            await BudgetLimitSteps.AddSpendingAsync(app, me, transport, 30_000, mkd, 62.0000m, "2026-09-12");
            await BudgetLimitSteps.AddSharedExpenseAsync(app, group, food, 20_000, mkd, 61.0000m, "2026-09-15");
            await BudgetLimitSteps.AddSharedExpenseAsync(app, group, transport, 2_000, eur, 61.0000m, "2026-09-20");
            await BudgetCurrencySteps.AddIncomeAsync(app, me, 4_000_000, mkd, 61.0000m, "2026-09-25");
        }
    }
}
