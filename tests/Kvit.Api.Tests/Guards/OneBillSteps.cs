using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public static class OneBillSteps
    {
        public const string MarkoName = "Marko";
        public const long WholeBillMinor = ZeroBalanceScenario.ExpenseMinor;
        public const long MarkoShareMinor = ZeroBalanceScenario.ShareMinor;
        public const long BojanShareMinor = 20_000;
        public const long BojanExpenseMinor = 40_000;

        public static async Task<OneBillSetup> CreateBillAsync(ExpensesApp app)
        {
            SignedInUser owner = await app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(WholeBillMinor, ExpenseInputs.Mkd, MarkoName);
            (Guid groupId, Guid expenseId) = await app.CreateOneBillAsync(owner, input);
            Guid ownerRowId = await GroupRows.CurrentMemberIdAsync(app, groupId, owner.UserId);
            Guid markoRowId = await app.MemberRowIdByNameAsync(groupId, MarkoName);

            return new OneBillSetup(groupId, owner, ownerRowId, markoRowId, expenseId);
        }

        public static async Task<(OneBillSetup Bill, SignedInUser Bojan, Guid BojanRowId)> CreateBillWithAnAccountMemberAsync(ExpensesApp app)
        {
            OneBillSetup bill = await CreateBillAsync(app);
            SignedInUser bojan = await app.SignUpAsync(GroupsApp.MemberName);
            DateTimeOffset joinedAt = app.Clock.GetUtcNow() + (ExpensesApp.JoiningGap * 5);
            Guid bojanRowId = await GroupRows.InsertMemberAsync(app, bill.GroupId, bojan.UserId, GroupsApp.MemberName, bill.Owner.UserId, joinedAt: joinedAt);
            ExpenseInput withBojan = ExpenseInputs.Equal(BojanExpenseMinor, ExpenseInputs.Mkd, bill.OwnerRowId, bill.OwnerRowId, bojanRowId);
            await app.AddExpenseAsync(bill.Owner, bill.GroupId, withBojan);

            return (bill, bojan, bojanRowId);
        }

        public static Task<Guid> MarkoPaysAsync(OneBillSetup bill, long amountMinor = MarkoShareMinor)
        {
            return SettlementSteps.RecordAsync(bill.Owner, bill.GroupId, bill.MarkoRowId, bill.OwnerRowId, amountMinor);
        }

        public static Task<Guid> BojanRecordsAPaymentAsync(OneBillSetup bill, SignedInUser bojan, Guid bojanRowId, long amountMinor)
        {
            return SettlementSteps.RecordAsync(bojan, bill.GroupId, bojanRowId, bill.OwnerRowId, amountMinor);
        }

        public static Task<Guid> OwnerRecordsBojanPayingAsync(OneBillSetup bill, Guid bojanRowId, long amountMinor)
        {
            return SettlementSteps.RecordAsync(bill.Owner, bill.GroupId, bojanRowId, bill.OwnerRowId, amountMinor);
        }

        public static ExpenseInput MarkoPaysEquallyInput(OneBillSetup bill, long amountMinor)
        {
            return ExpenseInputs.Equal(amountMinor, ExpenseInputs.Mkd, bill.MarkoRowId, bill.OwnerRowId, bill.MarkoRowId);
        }

        public static ExpenseInput OwnerPaysEquallyInput(OneBillSetup bill, long amountMinor)
        {
            return ExpenseInputs.Equal(amountMinor, ExpenseInputs.Mkd, bill.OwnerRowId, bill.OwnerRowId, bill.MarkoRowId);
        }

        public static async Task AssertStillOpenAsync(ExpensesApp app, Guid groupId)
        {
            Assert.Equal("Open", await ClosingRows.StatusAsync(app, groupId));
            Assert.Null(await ClosingRows.FinishedAtUtcAsync(app, groupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(app, groupId, "GroupFinished"));
        }

        public static async Task AssertFinishedNowByAsync(ExpensesApp app, Guid groupId, SignedInUser actor)
        {
            Assert.Equal("Finished", await ClosingRows.StatusAsync(app, groupId));
            Assert.Equal(SettlementRows.UtcTextOf(app.Clock.GetUtcNow()), await ClosingRows.FinishedAtUtcAsync(app, groupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(app, groupId, "GroupFinished"));
            Assert.Equal(actor.UserId.ToString(), await GroupRows.EventValueAsync(app, groupId, "GroupFinished", "actor_user_id"));
            Assert.Equal(SettlementRows.UtcTextOf(app.Clock.GetUtcNow()), await GroupRows.EventValueAsync(app, groupId, "GroupFinished", "created_at AT TIME ZONE 'UTC'"));
        }
    }
}
