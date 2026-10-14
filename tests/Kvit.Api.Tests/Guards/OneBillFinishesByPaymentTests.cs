using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public class OneBillFinishesByPaymentTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Record_APaymentThatPaysTheWholeDebt_FinishesTheBillAtOnceWithTheRecorderAsActor()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);

            await OneBillSteps.MarkoPaysAsync(bill);

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);
            Assert.Null(await GroupRows.GroupValueAsync(_app, bill.GroupId, "closing_started_at"));
        }

        [Fact]
        public async Task Record_TwoPaymentsThatTogetherPayTheDebt_FinishTheBillOnTheSecondOne()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);

            await OneBillSteps.MarkoPaysAsync(bill, 10_000);
            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);
            await OneBillSteps.MarkoPaysAsync(bill, 20_000);

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);
        }

        [Fact]
        public async Task Record_APaymentSmallerThanTheDebt_KeepsTheBillOpen()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);

            await OneBillSteps.MarkoPaysAsync(bill, 20_000);

            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);
        }

        [Fact]
        public async Task Record_APaymentLargerThanTheDebt_KeepsTheBillOpen()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);

            await OneBillSteps.MarkoPaysAsync(bill, 40_000);

            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);
        }

        [Fact]
        public async Task Record_APaymentThatClearsTheMkdDebtWhileAnEurDebtRemains_KeepsTheBillOpen()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);
            await _app.AddExpenseAsync(bill.Owner, bill.GroupId, ExpenseInputs.Equal(2_000, ExpenseInputs.Eur, bill.OwnerRowId, bill.OwnerRowId, bill.MarkoRowId));

            await OneBillSteps.MarkoPaysAsync(bill);

            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);
        }

        [Fact]
        public async Task Record_TheEurPaymentAfterTheMkdPayment_FinishesTheBill()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);
            await _app.AddExpenseAsync(bill.Owner, bill.GroupId, ExpenseInputs.Equal(2_000, ExpenseInputs.Eur, bill.OwnerRowId, bill.OwnerRowId, bill.MarkoRowId));
            await OneBillSteps.MarkoPaysAsync(bill);

            await SettlementSteps.RecordAsync(bill.Owner, bill.GroupId, bill.MarkoRowId, bill.OwnerRowId, 1_000, ExpenseInputs.Eur);

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);
        }

        [Fact]
        public async Task Confirm_TheLastPendingPaymentThatPaysTheDebt_FinishesTheBillWithTheConfirmerAsActor()
        {
            (OneBillSetup bill, SignedInUser bojan, Guid bojanRowId) = await OneBillSteps.CreateBillWithAnAccountMemberAsync(_app);
            await OneBillSteps.MarkoPaysAsync(bill);
            Guid pendingId = await OneBillSteps.BojanRecordsAPaymentAsync(bill, bojan, bojanRowId, OneBillSteps.BojanShareMinor);
            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);

            await SettlementSteps.ConfirmAsync(bill.Owner, bill.GroupId, pendingId);

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);
        }

        [Fact]
        public async Task Reject_APendingPaymentWhenEverythingElseIsPaid_FinishesTheBillWithTheRejecterAsActor()
        {
            (OneBillSetup bill, _, Guid pendingId) = await CreateBillPaidInFullExceptForAPendingPaymentByBojanAsync();
            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);

            await SettlementSteps.RejectAsync(bill.Owner, bill.GroupId, pendingId);

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);
        }

        [Fact]
        public async Task Cancel_APendingPaymentWhenEverythingElseIsPaid_FinishesTheBillWithThePayerAsActor()
        {
            (OneBillSetup bill, SignedInUser bojan, Guid pendingId) = await CreateBillPaidInFullExceptForAPendingPaymentByBojanAsync();
            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);

            await SettlementSteps.CancelAsync(bojan, bill.GroupId, pendingId);

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bojan);
        }

        [Fact]
        public async Task Reject_APendingPaymentThatLeavesADebt_KeepsTheBillOpen()
        {
            (OneBillSetup bill, SignedInUser bojan, Guid bojanRowId) = await OneBillSteps.CreateBillWithAnAccountMemberAsync(_app);
            await OneBillSteps.MarkoPaysAsync(bill);
            Guid pendingId = await OneBillSteps.BojanRecordsAPaymentAsync(bill, bojan, bojanRowId, OneBillSteps.BojanShareMinor);

            await SettlementSteps.RejectAsync(bill.Owner, bill.GroupId, pendingId);

            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);
        }

        [Fact]
        public async Task Delete_ThePaymentThatCausedAnOverpayment_FinishesTheBill()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);
            await OneBillSteps.MarkoPaysAsync(bill, 20_000);
            Guid overpayingPaymentId = await OneBillSteps.MarkoPaysAsync(bill, 40_000);
            await OneBillSteps.MarkoPaysAsync(bill, 10_000);
            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);

            await SettlementSteps.DeleteAsync(bill.Owner, bill.GroupId, overpayingPaymentId);

            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);
        }

        private async Task<(OneBillSetup Bill, SignedInUser Bojan, Guid PendingId)> CreateBillPaidInFullExceptForAPendingPaymentByBojanAsync()
        {
            (OneBillSetup bill, SignedInUser bojan, Guid bojanRowId) = await OneBillSteps.CreateBillWithAnAccountMemberAsync(_app);
            await OneBillSteps.MarkoPaysAsync(bill);
            Guid pendingId = await OneBillSteps.BojanRecordsAPaymentAsync(bill, bojan, bojanRowId, 5_000);
            await OneBillSteps.OwnerRecordsBojanPayingAsync(bill, bojanRowId, OneBillSteps.BojanShareMinor);
            await SettlementSteps.AssertAllBalancesZeroAsync(bill.Owner, bill.GroupId);

            return (bill, bojan, pendingId);
        }
    }
}
