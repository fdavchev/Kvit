using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.GroupSpending
{
    public class GetGroupSpendingCountedExpensesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_DeletedExpense_IsLeftOutOfTheTotalAndTheCategory()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 100_000, categoryId: GroupSpendingSteps.Food);
            Guid deleted = await GroupSpendingSteps.AddAsync(_app, group, 200_000, categoryId: GroupSpendingSteps.Food);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deleted);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(100_000, GroupSpendingJson.TotalOf(body, "MKD"));
            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Food, 100_000)], GroupSpendingJson.CategoriesOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_CategoryWhoseOnlyExpenseWasDeleted_IsNotListed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 100_000, categoryId: GroupSpendingSteps.Food);
            Guid deleted = await GroupSpendingSteps.AddAsync(_app, group, 200_000, categoryId: GroupSpendingSteps.Transport);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deleted);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Food, 100_000)], GroupSpendingJson.CategoriesOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_CurrencyWhoseOnlyExpenseWasDeleted_IsNotInTheAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 100_000);
            Guid deletedEur = await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deletedEur);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["MKD"], GroupSpendingJson.CurrencyCodesOf(body));
        }

        [Fact]
        public async Task Get_GroupWhoseOnlyExpenseWasDeleted_AnswersNoCurrencies()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await GroupSpendingSteps.AddAsync(_app, group, 100_000, categoryId: GroupSpendingSteps.Food);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(0, body.GetProperty("currencies").GetArrayLength());
        }

        [Fact]
        public async Task Get_DeletedThenRestoredExpense_IsCountedAgain()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, 100_000, categoryId: GroupSpendingSteps.Food);
            Guid expenseId = await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur, GroupSpendingSteps.Transport);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["MKD", "EUR"], GroupSpendingJson.CurrencyCodesOf(body));
            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Transport, 1_000)], GroupSpendingJson.CategoriesOf(body, "EUR"));
        }

        [Fact]
        public async Task Get_EditedExpense_CountsTheNewAmountCurrencyAndCategoryOnly()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await GroupSpendingSteps.AddAsync(_app, group, 100_000, categoryId: GroupSpendingSteps.Food);
            ExpenseInput edited = ExpenseInputs.Equal(3_000, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { CategoryId = GroupSpendingSteps.Transport };
            HttpResponseMessage edit = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, edited);
            Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["EUR"], GroupSpendingJson.CurrencyCodesOf(body));
            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Transport, 3_000)], GroupSpendingJson.CategoriesOf(body, "EUR"));
        }

        [Fact]
        public async Task Get_ConfirmedSettlement_DoesNotChangeTheTotals()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MarkoRowId));
            string before = await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            Assert.Equal(before, await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId));
        }

        [Fact]
        public async Task Get_PendingSettlement_DoesNotChangeTheTotals()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            string before = await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.PendingBojanToAnaAsync(group);

            Assert.Equal(before, await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId));
        }

        [Fact]
        public async Task Get_GroupWithOnlySettlementsAndNoExpenses_AnswersNoCurrencies()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            JsonElement body = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(0, body.GetProperty("currencies").GetArrayLength());
        }

        [Fact]
        public async Task Get_TwoGroupsOfTheSamePerson_DoNotMix()
        {
            ExpenseGroup first = await _app.CreateExpenseGroupAsync();
            Guid secondGroupId = await _app.CreateGroupAsync(first.Owner);
            Guid secondOwnerRowId = await GroupRows.CurrentMemberIdAsync(_app, secondGroupId, first.Owner.UserId);
            await GroupSpendingSteps.AddAsync(_app, first, 100_000, categoryId: GroupSpendingSteps.Food);
            await _app.AddExpenseAsync(first.Owner, secondGroupId, ExpenseInputs.Equal(7_000, ExpenseInputs.Eur, secondOwnerRowId, secondOwnerRowId) with { CategoryId = GroupSpendingSteps.Transport });

            JsonElement firstBody = await GroupSpendingRequests.ReadAsync(first.Owner.Client, first.GroupId);
            JsonElement secondBody = await GroupSpendingRequests.ReadAsync(first.Owner.Client, secondGroupId);

            Assert.Equal(["MKD"], GroupSpendingJson.CurrencyCodesOf(firstBody));
            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Food, 100_000)], GroupSpendingJson.CategoriesOf(firstBody, "MKD"));
            Assert.Equal(["EUR"], GroupSpendingJson.CurrencyCodesOf(secondBody));
            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Transport, 7_000)], GroupSpendingJson.CategoriesOf(secondBody, "EUR"));
        }

        [Fact]
        public async Task Get_OtherGroupsOfOtherPeople_NeverLeakIn()
        {
            ExpenseGroup mine = await _app.CreateExpenseGroupAsync();
            ExpenseGroup other = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, mine, 100_000, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, other, 555_000, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, other, 5_000, ExpenseInputs.Eur);

            JsonElement body = await GroupSpendingRequests.ReadAsync(mine.Owner.Client, mine.GroupId);

            Assert.Equal(["MKD"], GroupSpendingJson.CurrencyCodesOf(body));
            Assert.Equal(100_000, GroupSpendingJson.TotalOf(body, "MKD"));
        }

        [Fact]
        public async Task Get_OneBillGroup_AnswersTheSameWayAsAnyGroup()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko", "Petar") with { CategoryId = GroupSpendingSteps.Food };
            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input);

            JsonElement body = await GroupSpendingRequests.ReadAsync(caller.Client, groupId);

            Assert.Equal(["MKD"], GroupSpendingJson.CurrencyCodesOf(body));
            Assert.Equal(ExpenseInputs.OneThousandMkd, GroupSpendingJson.TotalOf(body, "MKD"));
            Assert.Equal([new CategoryTotal(GroupSpendingSteps.Food, ExpenseInputs.OneThousandMkd)], GroupSpendingJson.CategoriesOf(body, "MKD"));
            Assert.Equal(JsonValueKind.Null, body.GetProperty("plan").ValueKind);
        }

        [Fact]
        public async Task Get_OneBillGroupWithoutACategoryInEuro_AnswersTheNullCategoryRow()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(4_500, ExpenseInputs.Eur, "Marko");
            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input);

            JsonElement body = await GroupSpendingRequests.ReadAsync(caller.Client, groupId);

            Assert.Equal(["EUR"], GroupSpendingJson.CurrencyCodesOf(body));
            Assert.Equal([new CategoryTotal(null, 4_500)], GroupSpendingJson.CategoriesOf(body, "EUR"));
        }
    }
}
