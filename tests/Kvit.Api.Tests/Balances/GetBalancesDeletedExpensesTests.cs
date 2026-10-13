using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public class GetBalancesDeletedExpensesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_DeletedExpense_IsNotCounted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            Guid deleted = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(200_000, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deleted);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal([50_000L, -50_000L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([new ExpectedPayment(group.MemberRowId, group.OwnerRowId, 50_000)], BalanceJson.PaymentsOf(mkd));
        }

        [Fact]
        public async Task Get_CurrencyWhoseOnlyExpenseWasDeleted_IsNotInTheAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            Guid deletedEur = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_000, ExpenseInputs.Eur, group.MemberRowId, group.OwnerRowId, group.MemberRowId));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deletedEur);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["MKD"], BalanceJson.CurrencyCodesOf(body));
        }

        [Fact]
        public async Task Get_GroupWhoseOnlyExpenseWasDeleted_AnswersNoCurrenciesAndEveryoneIsKvit()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(0, body.GetProperty("currencies").GetArrayLength());
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_DeletedThenRestoredExpense_IsCountedAgain()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_000, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["EUR"], BalanceJson.CurrencyCodesOf(body));
            Assert.Equal([500L, -500L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "EUR")));
            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_EditedExpense_CountsTheNewAmountsOnly()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            ExpenseInput edited = ExpenseInputs.Equal(300_000, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId);
            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, edited);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal([-150_000L, 150_000L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
        }
    }
}
