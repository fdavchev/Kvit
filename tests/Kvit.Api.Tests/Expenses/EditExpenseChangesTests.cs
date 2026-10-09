using System.Net;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class EditExpenseChangesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const long TwelveHundredEurCents = 1_200;

        [Fact]
        public async Task Edit_Amount_WritesAnAmountEntryInMinorUnits()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { AmountMinor = 150_000 });

            (string Field, string Old, string New) amount = Assert.Single(changes, change => change.Field == "amount");
            Assert.Equal("120000", amount.Old);
            Assert.Equal("150000", amount.New);
            Assert.DoesNotContain(changes, change => change.Field is "currency" or "title" or "note" or "date" or "category" or "paidBy");
        }

        [Fact]
        public async Task Edit_Currency_WritesOnlyACurrencyEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { Currency = "EUR" });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("currency", change.Field);
            Assert.Equal("MKD", change.Old);
            Assert.Equal("EUR", change.New);
        }

        [Fact]
        public async Task Edit_Title_WritesOnlyATitleEntryWithTheOldAndNewTitle()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Title = "Dinner" };

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { Title = "Lunch" });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("title", change.Field);
            Assert.Equal("Dinner", change.Old);
            Assert.Equal("Lunch", change.New);
        }

        [Fact]
        public async Task Edit_TitleAddedWhereThereWasNone_WritesAnEmptyOldTitle()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { Title = "Lunch" });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("title", change.Field);
            Assert.Equal(string.Empty, change.Old);
            Assert.Equal("Lunch", change.New);
        }

        [Fact]
        public async Task Edit_TitleRemoved_WritesAnEmptyNewTitle()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Title = "Dinner" };

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { Title = null });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("title", change.Field);
            Assert.Equal("Dinner", change.Old);
            Assert.Equal(string.Empty, change.New);
        }

        [Fact]
        public async Task Edit_Note_WritesOnlyANoteEntryWithTheOldAndNewNote()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Note = "Lake" };

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { Note = "Sea" });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("note", change.Field);
            Assert.Equal("Lake", change.Old);
            Assert.Equal("Sea", change.New);
        }

        [Fact]
        public async Task Edit_NoteRemoved_WritesAnEmptyNewNote()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Note = "Lake" };

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { Note = null });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("note", change.Field);
            Assert.Equal("Lake", change.Old);
            Assert.Equal(string.Empty, change.New);
        }

        [Fact]
        public async Task Edit_Date_WritesOnlyADateEntryInYearMonthDayFormat()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { ExpenseDate = "2026-10-04" });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("date", change.Field);
            Assert.Equal("2026-10-05", change.Old);
            Assert.Equal("2026-10-04", change.New);
        }

        [Fact]
        public async Task Edit_CategoryAdded_WritesTheCategoryKeyAndAnEmptyOldValue()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { CategoryId = food });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("category", change.Field);
            Assert.Equal(string.Empty, change.Old);
            Assert.Equal("food", change.New);
        }

        [Fact]
        public async Task Edit_CategoryChanged_WritesTheOldAndNewKeys()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            Guid groceries = await _app.BuiltInCategoryIdAsync("groceries");
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { CategoryId = food };

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { CategoryId = groceries });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("category", change.Field);
            Assert.Equal("food", change.Old);
            Assert.Equal("groceries", change.New);
        }

        [Fact]
        public async Task Edit_CategoryRemoved_WritesAnEmptyNewValue()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { CategoryId = food };

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { CategoryId = null });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("category", change.Field);
            Assert.Equal("food", change.Old);
            Assert.Equal(string.Empty, change.New);
        }

        [Fact]
        public async Task Edit_PayerChangedToAnotherAccount_WritesThePayersNames()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { PaidByMemberId = group.MemberRowId });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("paidBy", change.Field);
            Assert.Equal(GroupsApp.OwnerName, change.Old);
            Assert.Equal(GroupsApp.MemberName, change.New);
        }

        [Fact]
        public async Task Edit_PayerChangedToAPlainName_WritesThePlainName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { PaidByMemberId = group.PlainRowIds[0] });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("paidBy", change.Field);
            Assert.Equal(GroupsApp.OwnerName, change.Old);
            Assert.Equal("Marko", change.New);
        }

        [Fact]
        public async Task Edit_SplitTypeChangedWithTheSameShares_WritesOneSplitEntryWithTypeAndEachPersonsShare()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(TwelveHundredEurCents, "EUR", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            ExpenseInput changed = ExpenseInputs.Of("Shares", TwelveHundredEurCents, "EUR", group.OwnerRowId, new ShareInput(group.OwnerRowId, 1), new ShareInput(group.MemberRowId, 1));

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, changed);

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("split", change.Field);
            Assert.Equal("Equal: Ana 600, Bojan 600", change.Old);
            Assert.Equal("Shares: Ana 600, Bojan 600", change.New);
        }

        [Fact]
        public async Task Edit_SplitAmongThreeInsteadOfTwo_WritesOneSplitEntryWithTheOldAndNewShares()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput original = ExpenseInputs.Equal(TwelveHundredEurCents, "EUR", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            ExpenseInput changed = ExpenseInputs.Equal(TwelveHundredEurCents, "EUR", group.OwnerRowId, group.PlainRowIds[0], group.MemberRowId, group.OwnerRowId);

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, changed);

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("split", change.Field);
            Assert.Equal("Equal: Ana 600, Bojan 600", change.Old);
            Assert.Equal("Equal: Ana 400, Bojan 400, Marko 400", change.New);
        }

        [Fact]
        public async Task Edit_TitleAndNoteAndDate_WritesOneEntryForEachInOneEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Title = "Dinner", Note = "Lake" };
            ExpenseInput changed = original with { Title = "Lunch", Note = "Sea", ExpenseDate = "2026-10-04" };

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, changed);

            Assert.Equal(["date", "note", "title"], [.. changes.Select(change => change.Field).Order(StringComparer.Ordinal)]);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseEdited"));
        }

        [Fact]
        public async Task Edit_EveryTrackedFieldAtOnce_WritesEightEntries()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Title = "Dinner", Note = "Lake" };
            ShareInput[] sharesWithAnExtra = [new ShareInput(group.OwnerRowId, 0), new ShareInput(group.MemberRowId, 100)];
            ExpenseInput changed = ExpenseInputs.Of("Equal", TwelveHundredEurCents + 100, "EUR", group.MemberRowId, sharesWithAnExtra) with
            {
                Title = "Lunch",
                Note = "Sea",
                ExpenseDate = "2026-10-04",
                CategoryId = food,
            };

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, changed);

            Assert.Equal(
                ["amount", "category", "currency", "date", "note", "paidBy", "split", "title"],
                [.. changes.Select(change => change.Field).Order(StringComparer.Ordinal)]);
            (string Field, string Old, string New) split = Assert.Single(changes, change => change.Field == "split");
            Assert.Equal("Equal: Ana 60000, Bojan 60000", split.Old);
            Assert.Equal("Equal: Ana 600, Bojan 700", split.New);
        }

        [Fact]
        public async Task Edit_AmountOnlyOfAnEqualSplit_WritesAnAmountEntryAndNoSplitEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, original, original with { AmountMinor = 150_000 });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("amount", change.Field);
            Assert.Equal("120000", change.Old);
            Assert.Equal("150000", change.New);
        }

        [Fact]
        public async Task Edit_SharesListedInAnotherOrder_IsNoChangeAndWritesNoEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput original = ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, ExpenseInputs.Equal(120_000, "MKD", group.OwnerRowId, group.PlainRowIds[0], group.MemberRowId, group.OwnerRowId));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseEdited"));
        }

        private async Task<List<(string Field, string Old, string New)>> EditAndReadChangesAsync(ExpenseGroup group, ExpenseInput original, ExpenseInput changed)
        {
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, original);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, changed);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            string? changesJson = await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseEdited", "changes");
            return ActivityChanges.Parse(changesJson);
        }
    }
}
