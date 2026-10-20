using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class ListRecurringIncomesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task List_NewUser_Answers200WithOnlyTheRecurringIncomesPropertyAndNoRows()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await RecurringIncomeRequests.ListAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal([RecurringIncomeSteps.ListProperty], body.EnumerateObject().Select(property => property.Name));
            Assert.Empty(body.GetProperty(RecurringIncomeSteps.ListProperty).EnumerateArray());
        }

        [Fact]
        public async Task List_EachRow_HasExactlyTheTenFields()
        {
            SignedInUser user = await _app.SignUpAsync();
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Weekly());

            List<JsonElement> rows = await RecurringIncomeSteps.ListRowsAsync(user.Client);

            Assert.Equal(2, rows.Count);
            Assert.All(rows, row => Assert.Equal(RecurringIncomeRequests.RowFieldNames, row.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)));
        }

        [Fact]
        public async Task List_ARow_HasTheSameShapeAsTheCreateAnswer()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpResponseMessage created = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(endsOn: "2027-03-31"));
            JsonElement createdBody = await AuthRequests.ReadJsonAsync(created);

            List<JsonElement> rows = await RecurringIncomeSteps.ListRowsAsync(user.Client);

            Assert.Equal(createdBody.GetRawText(), Assert.Single(rows).GetRawText());
        }

        [Fact]
        public async Task List_WeeklyRow_HasTheWeekdayAndANullDayOfMonth()
        {
            SignedInUser user = await _app.SignUpAsync();
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Weekly(dayOfWeek: 6));

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal("Weekly", row.GetProperty("frequency").GetString());
            Assert.Equal(6, row.GetProperty("dayOfWeek").GetInt32());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("dayOfMonth").ValueKind);
        }

        [Fact]
        public async Task List_SeveralIncomes_AreNewestFirst()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid oldest = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Oldest"));
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            Guid middle = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Weekly(title: "Middle"));
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            Guid newest = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Newest"));

            List<Guid> ids = await RecurringIncomeSteps.ListedIdsAsync(user.Client);

            Assert.Equal([newest, middle, oldest], ids);
        }

        [Fact]
        public async Task List_AStoppedIncome_IsNotListed()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid active = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Salary"));
            await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Weekly(title: "Old job"));

            List<Guid> ids = await RecurringIncomeSteps.ListedIdsAsync(user.Client);

            Assert.Equal([active], ids);
        }

        [Fact]
        public async Task List_ARestartedIncome_IsListedAgain()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeRequests.RestartAsync(user.Client, recurringIncomeId);

            List<Guid> ids = await RecurringIncomeSteps.ListedIdsAsync(user.Client);

            Assert.Equal([recurringIncomeId], ids);
        }

        [Fact]
        public async Task List_ShowsOnlyTheCallersIncomes()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            Guid ofFirst = await RecurringIncomeSteps.CreateAsync(first.Client, RecurringIncomeBody.Monthly(title: "First"));
            Guid ofSecond = await RecurringIncomeSteps.CreateAsync(second.Client, RecurringIncomeBody.Weekly(title: "Second"));

            Assert.Equal([ofFirst], await RecurringIncomeSteps.ListedIdsAsync(first.Client));
            Assert.Equal([ofSecond], await RecurringIncomeSteps.ListedIdsAsync(second.Client));
        }

        [Fact]
        public async Task List_ChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            string rowsBefore = await RecurringIncomeRows.SnapshotAsync(_app);
            string entriesBefore = await RecurringIncomeRows.EntriesSnapshotAsync(_app);

            await RecurringIncomeSteps.ListRowsAsync(user.Client);

            Assert.Equal(rowsBefore, await RecurringIncomeRows.SnapshotAsync(_app));
            Assert.Equal(entriesBefore, await RecurringIncomeRows.EntriesSnapshotAsync(_app));
        }
    }
}
