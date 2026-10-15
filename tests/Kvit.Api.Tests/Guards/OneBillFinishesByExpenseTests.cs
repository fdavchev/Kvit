using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public class OneBillFinishesByExpenseTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task AddExpense_ThatCancelsOutTheRestOfTheDebt_FinishesTheBillWithTheAdderAsActor()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);
            await OneBillSteps.MarkoPaysAsync(bill, 10_000);

            await _app.AddExpenseAsync(bill.Owner, bill.GroupId, OneBillSteps.MarkoPaysEquallyInput(bill, 40_000));

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);
        }

        [Fact]
        public async Task AddExpense_ThatMakesBalancesZeroWhileNoPaymentWasMade_KeepsTheBillOpen()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);

            await _app.AddExpenseAsync(bill.Owner, bill.GroupId, OneBillSteps.MarkoPaysEquallyInput(bill, OneBillSteps.WholeBillMinor));

            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);
        }

        [Fact]
        public async Task EditExpense_ThatLowersTheBillToWhatWasPaid_FinishesTheBill()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);
            await OneBillSteps.MarkoPaysAsync(bill, 20_000);
            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);

            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.EditAsync(bill.Owner.Client, bill.GroupId, bill.ExpenseId, OneBillSteps.OwnerPaysEquallyInput(bill, 40_000)));

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);
        }

        [Fact]
        public async Task DeleteExpense_ThatRemovesTheExtraDebt_FinishesTheBill()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);
            Guid extraExpenseId = await _app.AddExpenseAsync(bill.Owner, bill.GroupId, OneBillSteps.OwnerPaysEquallyInput(bill, 20_000));
            await OneBillSteps.MarkoPaysAsync(bill);
            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);

            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.DeleteAsync(bill.Owner.Client, bill.GroupId, extraExpenseId));

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);
        }

        [Fact]
        public async Task RestoreExpense_ThatCancelsOutTheRestOfTheDebt_FinishesTheBill()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);
            Guid deletedExpenseId = await _app.AddExpenseAsync(bill.Owner, bill.GroupId, OneBillSteps.MarkoPaysEquallyInput(bill, 40_000));
            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.DeleteAsync(bill.Owner.Client, bill.GroupId, deletedExpenseId));
            await OneBillSteps.MarkoPaysAsync(bill, 10_000);
            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);

            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.RestoreAsync(bill.Owner.Client, bill.GroupId, deletedExpenseId));

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);
        }
    }
}
