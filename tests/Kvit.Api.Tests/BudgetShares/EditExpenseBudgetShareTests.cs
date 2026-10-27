using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.BudgetShares
{
    public class EditExpenseBudgetShareTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const long TwoThousandMkd = 200_000;

        [Fact]
        public async Task Edit_ShareThatAlreadyExisted_KeepsTrueWhenTheSwitchWentOffAfterTheSave()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            ExpenseInput changed = await BudgetShareSteps.SharedInputAsync(_app, group, amountMinor: TwoThousandMkd) with { Title = "Dinner for four" };

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, changed);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.True(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.MemberRowId));
            JsonElement row = Assert.Single((await BudgetShareSteps.GroupSharesAsync(group.Member.Client)).EnumerateArray());
            Assert.Equal(expenseId, row.GetProperty("expenseId").GetGuid());
            Assert.Equal("Dinner for four", row.GetProperty("title").GetString());
            Assert.Equal(100_000, row.GetProperty("shareMinor").GetInt64());
        }

        [Fact]
        public async Task Edit_ShareThatAlreadyExisted_KeepsFalseWhenTheSwitchWentOnAfterTheSave()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, true);
            ExpenseInput changed = await BudgetShareSteps.SharedInputAsync(_app, group, amountMinor: TwoThousandMkd);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, changed);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.MemberRowId));
            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
        }

        [Fact]
        public async Task Edit_NewShareOfAMemberWhoseSwitchIsOff_IsStoredAsNotCountedWhileTheOldShareKeepsItsFlag()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput onlyOwner = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { CategoryId = food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, onlyOwner);
            ExpenseInput withMember = await BudgetShareSteps.SharedInputAsync(_app, group);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, withMember);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.MemberRowId));
            Assert.True(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.OwnerRowId));
            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Owner.Client));
        }

        [Fact]
        public async Task Edit_NewShareOfAMemberWhoseSwitchIsOn_UsesTheMembersSwitchAndNotTheEditors()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Owner, group.GroupId, false);
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput onlyOwner = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { CategoryId = food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, onlyOwner);
            ExpenseInput withMember = await BudgetShareSteps.SharedInputAsync(_app, group);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, withMember);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.True(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.MemberRowId));
            Assert.False(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.OwnerRowId));
            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Owner.Client));
        }

        [Fact]
        public async Task Edit_MemberRemovedInOneEditAndAddedBackInALaterOne_GetsANewShareWithTheCurrentSwitch()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput onlyOwner = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { CategoryId = food };
            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, onlyOwner);
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, await BudgetShareSteps.SharedInputAsync(_app, group));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.MemberRowId));
            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
        }

        [Fact]
        public async Task Edit_SplitTypeAndAmountsChanged_KeepsEachMembersOwnFlag()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Owner, group.GroupId, false);
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 20_000), new ShareInput(group.MemberRowId, 80_000)];
            ExpenseInput exact = ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares) with { CategoryId = await _app.BuiltInCategoryIdAsync("food") };

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, exact);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.OwnerRowId));
            Assert.True(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.MemberRowId));
            JsonElement memberRow = Assert.Single((await BudgetShareSteps.GroupSharesAsync(group.Member.Client)).EnumerateArray());
            Assert.Equal(80_000, memberRow.GetProperty("shareMinor").GetInt64());
            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Owner.Client));
        }

        [Fact]
        public async Task Edit_Date_MovesTheShareToTheOtherMonth()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-05");
            ExpenseInput moved = await BudgetShareSteps.SharedInputAsync(_app, group, "2026-11-02");

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, moved);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client, "2026-10"));
            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client, "2026-11"));
        }

        [Fact]
        public async Task Edit_CurrencyToEur_ListsTheShareInEuroAndKeepsTheFlag()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            ExpenseInput inEur = await BudgetShareSteps.SharedInputAsync(_app, group, amountMinor: 2_000, currency: ExpenseInputs.Eur);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, inEur);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            JsonElement row = Assert.Single((await BudgetShareSteps.GroupSharesAsync(group.Member.Client)).EnumerateArray());
            Assert.Equal(ExpenseInputs.Eur, row.GetProperty("currency").GetString());
            Assert.Equal(1_000, row.GetProperty("shareMinor").GetInt64());
            Assert.True(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.MemberRowId));
        }
    }
}
