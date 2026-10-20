using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CatchUpVisibilityTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task CatchUp_CreatedEntries_AreListedInTheirMonthAsIncomeWithTheTitleAndNoCategory()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeRows.InsertAsync(_app, user.UserId, title: "Salary", dayOfMonth: 15, startsOn: new DateOnly(2026, 9, 1));
            await CatchUpSteps.CatchUpAsync(user.Client);

            JsonElement october = await PersonalEntrySteps.MonthAsync(user.Client, "2026-10");
            JsonElement september = await PersonalEntrySteps.MonthAsync(user.Client, "2026-09");

            JsonElement entry = Assert.Single(october.GetProperty("entries").EnumerateArray());
            Assert.Equal("Income", entry.GetProperty("kind").GetString());
            Assert.Equal("Salary", entry.GetProperty("note").GetString());
            Assert.Equal("2026-10-15", entry.GetProperty("entryDate").GetString());
            Assert.Equal(4_500_000, entry.GetProperty("amountMinor").GetInt64());
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("categoryId").ValueKind);
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("category").ValueKind);
            Assert.Equal("2026-09-15", Assert.Single(september.GetProperty("entries").EnumerateArray()).GetProperty("entryDate").GetString());
        }

        [Fact]
        public async Task CatchUp_CreatedEntries_AddUpInTheIncomeTotalsPerCurrencyAndNotInTheSpendingTotals()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeRows.InsertAsync(_app, user.UserId, amountMinor: 4_500_000, currency: "MKD", dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            await RecurringIncomeRows.InsertAsync(_app, user.UserId, amountMinor: 20_000, currency: "EUR", dayOfMonth: 10, startsOn: new DateOnly(2026, 10, 1));
            await RecurringIncomeRows.InsertAsync(_app, user.UserId, amountMinor: 500_000, currency: "MKD", frequency: "Weekly", dayOfMonth: null, dayOfWeek: 3, startsOn: new DateOnly(2026, 10, 1));
            await CatchUpSteps.CatchUpAsync(user.Client);

            JsonElement october = await PersonalEntrySteps.MonthAsync(user.Client, "2026-10");

            JsonElement incomeTotals = october.GetProperty("incomeTotals");
            Assert.Equal(2, incomeTotals.GetArrayLength());
            Assert.Equal("MKD", incomeTotals[0].GetProperty("currency").GetString());
            Assert.Equal(4_500_000 + (2 * 500_000), incomeTotals[0].GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
            Assert.Equal(3, incomeTotals[0].GetProperty("count").GetInt32());
            Assert.Equal("EUR", incomeTotals[1].GetProperty("currency").GetString());
            Assert.Equal(20_000, incomeTotals[1].GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
            Assert.Equal(1, incomeTotals[1].GetProperty("count").GetInt32());
            Assert.Equal(0, october.GetProperty("totals").GetArrayLength());
            Assert.Equal(4, october.GetProperty("entries").GetArrayLength());
        }

        [Fact]
        public async Task CatchUp_CreatedEntry_CanBeDeletedAndRestoredLikeAnyOtherIncomeEntry()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            await CatchUpSteps.CatchUpAsync(user.Client);
            Guid entryId = Assert.Single(await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));

            HttpResponseMessage deleted = await PersonalEntryRequests.DeleteAsync(user.Client, entryId);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            Assert.Empty(await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));
            HttpResponseMessage restored = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
            Assert.Equal([entryId], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));
        }
    }
}
