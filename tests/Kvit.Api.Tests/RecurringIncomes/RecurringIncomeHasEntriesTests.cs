using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class RecurringIncomeHasEntriesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string HasEntriesProperty = "hasEntries";

        [Fact]
        public async Task Create_NewIncome_AnswersHasEntriesFalse()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15, startsOn: "2026-08-01"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonValueKind.False, (await AuthRequests.ReadJsonAsync(response)).GetProperty(HasEntriesProperty).ValueKind);
        }

        [Fact]
        public async Task List_BeforeAndAfterTheCatchUpCreatedEntries_HasEntriesTurnsFromFalseToTrue()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15, startsOn: "2026-08-01"));
            JsonElement before = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            await CatchUpSteps.CatchUpAsync(user.Client);

            JsonElement after = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));
            Assert.Equal(JsonValueKind.False, before.GetProperty(HasEntriesProperty).ValueKind);
            Assert.Equal(JsonValueKind.True, after.GetProperty(HasEntriesProperty).ValueKind);
        }

        [Fact]
        public async Task List_IncomeStartingInTheFuture_HasEntriesStaysFalseAfterTheCatchUp()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 1, startsOn: "2026-12-01"));

            await CatchUpSteps.CatchUpAsync(user.Client);

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));
            Assert.Equal(JsonValueKind.False, row.GetProperty(HasEntriesProperty).ValueKind);
        }

        [Fact]
        public async Task List_OnlyADeletedEntryExists_HasEntriesIsTrueLikeTheStartDateRule()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 15), deletedAt: _app.Clock.GetUtcNow());

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal(JsonValueKind.True, row.GetProperty(HasEntriesProperty).ValueKind);
        }

        [Fact]
        public async Task List_EntriesOfAnotherIncome_DoNotCountForThisOne()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid withEntry = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            Guid withoutEntry = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 20, startsOn: new DateOnly(2026, 11, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, withEntry, new DateOnly(2026, 10, 15));

            List<JsonElement> rows = await RecurringIncomeSteps.ListRowsAsync(user.Client);

            Assert.Equal(JsonValueKind.True, rows.Single(row => row.GetProperty("id").GetGuid() == withEntry).GetProperty(HasEntriesProperty).ValueKind);
            Assert.Equal(JsonValueKind.False, rows.Single(row => row.GetProperty("id").GetGuid() == withoutEntry).GetProperty(HasEntriesProperty).ValueKind);
        }

        [Fact]
        public async Task Edit_IncomeWithEntries_AnswersHasEntriesTrue()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15, startsOn: "2026-08-01"));
            await CatchUpSteps.CatchUpAsync(user.Client);

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(5_000_000, dayOfMonth: 15, startsOn: "2026-08-01"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonValueKind.True, (await AuthRequests.ReadJsonAsync(response)).GetProperty(HasEntriesProperty).ValueKind);
        }
    }
}
