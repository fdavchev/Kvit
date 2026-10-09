using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class EditExpenseEventDataTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task Edit_OfAnMkdExpense_WritesTheEventDataWithOnlyTheCurrencyMkd()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);

            await EditAsync(group, expenseId, original with { AmountMinor = 150_000 });

            JsonElement data = await StoredEditedEventDataAsync(group);
            Assert.Equal(["currency"], GroupRequests.PropertyNamesOf(data));
            Assert.Equal("MKD", data.GetProperty("currency").GetString());
        }

        [Fact]
        public async Task Edit_OfAnEurExpense_WritesTheEventDataWithOnlyTheCurrencyEur()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(1_250, "EUR", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);

            await EditAsync(group, expenseId, original with { AmountMinor = 2_000 });

            JsonElement data = await StoredEditedEventDataAsync(group);
            Assert.Equal(["currency"], GroupRequests.PropertyNamesOf(data));
            Assert.Equal("EUR", data.GetProperty("currency").GetString());
        }

        [Fact]
        public async Task Edit_OfAnEurExpenseThatOnlyChangesTheTitle_WritesTheCurrencyEurToo()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(1_250, "EUR", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);

            await EditAsync(group, expenseId, original with { Title = "Lunch" });

            Assert.Equal("EUR", (await StoredEditedEventDataAsync(group)).GetProperty("currency").GetString());
        }

        [Fact]
        public async Task Edit_ThatChangesTheCurrencyFromMkdToEur_WritesTheNewCurrencyInTheDataAndKeepsOldAndNewInTheCurrencyChange()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);

            await EditAsync(group, expenseId, original with { Currency = "EUR", AmountMinor = 2_000 });

            JsonElement data = await StoredEditedEventDataAsync(group);
            Assert.Equal(["currency"], GroupRequests.PropertyNamesOf(data));
            Assert.Equal("EUR", data.GetProperty("currency").GetString());
            List<(string Field, string Old, string New)> changes = ActivityChanges.Parse(await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseEdited", "changes"));
            (string Field, string Old, string New) currency = Assert.Single(changes, change => change.Field == "currency");
            Assert.Equal("MKD", currency.Old);
            Assert.Equal("EUR", currency.New);
            Assert.Contains(changes, change => change.Field == "amount");
        }

        [Fact]
        public async Task Edit_ThatChangesTheCurrencyFromEurToMkd_WritesTheNewCurrencyInTheDataAndKeepsOldAndNewInTheCurrencyChange()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(1_250, "EUR", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);

            await EditAsync(group, expenseId, original with { Currency = "MKD", AmountMinor = 120_000 });

            Assert.Equal("MKD", (await StoredEditedEventDataAsync(group)).GetProperty("currency").GetString());
            List<(string Field, string Old, string New)> changes = ActivityChanges.Parse(await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseEdited", "changes"));
            (string Field, string Old, string New) currency = Assert.Single(changes, change => change.Field == "currency");
            Assert.Equal("EUR", currency.Old);
            Assert.Equal("MKD", currency.New);
        }

        [Fact]
        public async Task Activity_EditEvent_AnswersTheDataWithTheCurrencyAfterTheEdit()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);
            _app.Clock.Advance(OneMinute);
            await EditAsync(group, expenseId, original with { Currency = "EUR", AmountMinor = 2_000 });

            JsonElement entry = (await ReadActivityAsync(group))[0];

            Assert.Equal("ExpenseEdited", entry.GetProperty("type").GetString());
            JsonElement data = entry.GetProperty("data");
            Assert.Equal(JsonValueKind.Object, data.ValueKind);
            Assert.Equal(["currency"], GroupRequests.PropertyNamesOf(data));
            Assert.Equal("EUR", data.GetProperty("currency").GetString());
        }

        [Fact]
        public async Task Activity_EditsThatSwitchTheCurrencyBackAndForth_EachAnswerTheCurrencyAfterItsOwnEdit()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);
            _app.Clock.Advance(OneMinute);
            await EditAsync(group, expenseId, original with { Currency = "EUR", AmountMinor = 2_000 });
            _app.Clock.Advance(OneMinute);
            await EditAsync(group, expenseId, original with { Currency = "MKD", AmountMinor = 130_000 });
            _app.Clock.Advance(OneMinute);
            await EditAsync(group, expenseId, original with { Currency = "MKD", AmountMinor = 140_000 });

            JsonElement events = await ReadActivityAsync(group);

            List<string> currencies = [.. events.EnumerateArray()
                .Where(entry => entry.GetProperty("type").GetString() == "ExpenseEdited")
                .Select(entry => entry.GetProperty("data").GetProperty("currency").GetString()!)];
            Assert.Equal(["MKD", "MKD", "EUR"], currencies);
        }

        [Fact]
        public async Task Get_HistoryOfAnEditedExpense_KeepsExactlyTheFourFieldsOfAnEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(1_250, "EUR", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);
            await EditAsync(group, expenseId, original with { AmountMinor = 2_000 });

            JsonElement history = (await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId)).GetProperty("history");

            Assert.All(history.EnumerateArray(), entry => Assert.Equal(ExpenseJson.HistoryFieldNames, GroupRequests.PropertyNamesOf(entry)));
        }

        [Fact]
        public async Task Edit_ThatChangesNothingOnAnEurExpense_WritesNoEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(1_250, "EUR", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, original);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseEdited"));
            Assert.DoesNotContain("ExpenseEdited", ExpenseJson.TypesOf(await ReadActivityAsync(group)));
        }

        [Fact]
        public async Task Activity_ExpenseAddedEvent_KeepsTheDataWithExactlyTitleAmountMinorAndCurrency()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_550, "EUR", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" });

            JsonElement data = (await ReadActivityAsync(group))[0].GetProperty("data");

            Assert.Equal(["amountMinor", "currency", "title"], GroupRequests.PropertyNamesOf(data));
        }

        [Fact]
        public async Task Activity_ExpenseDeletedEvent_KeepsTheDataWithExactlyTitleAmountMinorAndCurrency()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_550, "EUR", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" });
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement entry = (await ReadActivityAsync(group))[0];

            Assert.Equal("ExpenseDeleted", entry.GetProperty("type").GetString());
            Assert.Equal(["amountMinor", "currency", "title"], GroupRequests.PropertyNamesOf(entry.GetProperty("data")));
        }

        [Fact]
        public async Task Activity_ExpenseRestoredEvent_KeepsNullData()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_550, "EUR", group.OwnerRowId, group.OwnerRowId));
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement entry = (await ReadActivityAsync(group))[0];

            Assert.Equal("ExpenseRestored", entry.GetProperty("type").GetString());
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("data").ValueKind);
        }

        private async Task EditAsync(ExpenseGroup group, Guid expenseId, ExpenseInput changed)
        {
            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, changed);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private async Task<JsonElement> StoredEditedEventDataAsync(ExpenseGroup group)
        {
            string? dataJson = await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseEdited", "data");

            Assert.NotNull(dataJson);
            return JsonDocument.Parse(dataJson).RootElement;
        }

        private async Task<JsonElement> ReadActivityAsync(ExpenseGroup group)
        {
            HttpResponseMessage response = await ExpenseRequests.ActivityAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("events");
        }
    }
}
