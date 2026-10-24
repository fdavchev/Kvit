using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.BudgetCurrency
{
    public static class BudgetCurrencySteps
    {
        public static readonly string[] ConvertedTotalsFieldNames = ["currency", "groupSharesMinor", "incomeMinor", "leftMinor", "spendingMinor"];

        private static readonly DateOnly RateDate = new(2026, 10, 2);

        public static async Task SetAsync(SignedInUser user, string? budgetTotalCurrency)
        {
            HttpResponseMessage response = await BudgetCurrencyRequests.SetAsync(user.Client, budgetTotalCurrency);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public static async Task<string?> StoredAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> values = await database.QueryAsync(
                "SELECT budget_total_currency FROM users WHERE id = @id",
                new NpgsqlParameter("id", userId));

            return Assert.Single(values);
        }

        public static async Task<JsonElement> ConvertedTotalsAsync(HttpClient client, string month = PersonalEntryRequests.DefaultMonth)
        {
            JsonElement body = await PersonalEntrySteps.MonthAsync(client, month);

            return body.GetProperty("convertedTotals");
        }

        public static void AssertConvertedTotals(JsonElement convertedTotals, string currency, long spendingMinor, long incomeMinor, long groupSharesMinor, long leftMinor)
        {
            Assert.Equal(JsonValueKind.Object, convertedTotals.ValueKind);
            Assert.Equal(ConvertedTotalsFieldNames, GroupRequests.PropertyNamesOf(convertedTotals));
            Assert.Equal(currency, convertedTotals.GetProperty("currency").GetString());
            Assert.Equal(spendingMinor, convertedTotals.GetProperty("spendingMinor").GetInt64());
            Assert.Equal(incomeMinor, convertedTotals.GetProperty("incomeMinor").GetInt64());
            Assert.Equal(groupSharesMinor, convertedTotals.GetProperty("groupSharesMinor").GetInt64());
            Assert.Equal(leftMinor, convertedTotals.GetProperty("leftMinor").GetInt64());
        }

        public static Task UseRateAsync(ExpensesApp app, decimal mkdPerEur)
        {
            return app.PrepareRatesAsync(RateDate, mkdPerEur, TimeSpan.FromHours(1));
        }

        public static async Task<Guid> AddSpendingAsync(ExpensesApp app, SignedInUser user, long amountMinor, string currency, decimal mkdPerEur, string entryDate = PersonalEntryRequests.DefaultDate)
        {
            await UseRateAsync(app, mkdPerEur);

            return await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, amountMinor, currency, entryDate));
        }

        public static async Task<Guid> AddIncomeAsync(ExpensesApp app, SignedInUser user, long amountMinor, string currency, decimal mkdPerEur, string entryDate = PersonalEntryRequests.DefaultDate)
        {
            await UseRateAsync(app, mkdPerEur);

            return await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(amountMinor, currency, entryDate));
        }

        public static async Task<Guid> AddSharedExpenseAsync(ExpensesApp app, ExpenseGroup group, long amountMinor, string currency, decimal mkdPerEur, string expenseDate = ExpenseInputs.DefaultDate)
        {
            await UseRateAsync(app, mkdPerEur);

            return await BudgetShareSteps.AddSharedExpenseAsync(app, group, expenseDate, amountMinor, currency);
        }

        public static async Task AddMixedMonthAsync(ExpensesApp app, ExpenseGroup group)
        {
            SignedInUser me = group.Member;
            await AddSpendingAsync(app, me, 100_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await AddSpendingAsync(app, me, 50_000, BudgetCurrencyRequests.Mkd, 62.0000m);
            await AddSpendingAsync(app, me, 2_000, BudgetCurrencyRequests.Eur, 61.5610m);
            await AddSpendingAsync(app, me, 1_234, BudgetCurrencyRequests.Eur, 61.5000m);
            await AddSpendingAsync(app, me, 1_234, BudgetCurrencyRequests.Eur, 62.0000m);
            await AddIncomeAsync(app, me, 4_500_000, BudgetCurrencyRequests.Mkd, 61.2500m);
            await AddIncomeAsync(app, me, 1_500_000, BudgetCurrencyRequests.Mkd, 62.5000m);
            await AddIncomeAsync(app, me, 10_000, BudgetCurrencyRequests.Eur, 61.8123m);
            await AddSharedExpenseAsync(app, group, 100_000, BudgetCurrencyRequests.Mkd, 61.4000m);
            await AddSharedExpenseAsync(app, group, 1_000, BudgetCurrencyRequests.Eur, 61.7000m);
            await AddSharedExpenseAsync(app, group, 1_000, BudgetCurrencyRequests.Eur, 62.3000m);
        }
    }
}
