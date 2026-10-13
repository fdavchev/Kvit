using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class DeleteSettlementEndedMemberTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(PaymentSide.Payer, EndedMemberScenario.Left)]
        [InlineData(PaymentSide.Payer, EndedMemberScenario.Removed)]
        [InlineData(PaymentSide.Receiver, EndedMemberScenario.Left)]
        [InlineData(PaymentSide.Receiver, EndedMemberScenario.Removed)]
        public async Task Delete_PaymentOfAMemberWhoHasLeftOrWasRemoved_Answers400MemberNotCurrentAndChangesNothing(PaymentSide bojanIs, string endKind)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await EndedMemberScenario.RecordPaymentThatSettlesTheDebtAsync(_app, group, bojanIs);
            await EndedMemberScenario.EndBojanMembershipAsync(group, endKind);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_by_user_id"));
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(0, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementDeleted"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Delete_PaymentOfAPlainNameWhoWasRemoved_Answers400MemberNotCurrentAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId);
            await EndedMemberScenario.RemoveMarkoAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Delete_PaymentWhenBothMembersAreCurrentAndBojanPaid_Answers204AndPutsTheDebtBack()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await EndedMemberScenario.RecordPaymentThatSettlesTheDebtAsync(_app, group, PaymentSide.Payer);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.NotNull(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Delete_PaymentWhenBothMembersAreCurrentAndBojanWasPaid_Answers204AndPutsTheDebtBack()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await EndedMemberScenario.RecordPaymentThatSettlesTheDebtAsync(_app, group, PaymentSide.Receiver);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.NotNull(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Delete_PaymentOfAPlainNameWhoIsStillAMember_Answers204AndPutsTheDebtBack()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.NotNull(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Equal([30_000L, 0L, -30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Theory]
        [InlineData(EndedMemberScenario.Left)]
        [InlineData(EndedMemberScenario.Removed)]
        public async Task Delete_PaymentBetweenTwoCurrentMembersWhileAnotherMemberHasEnded_Answers204(string endKind)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AddTwoPersonExpenseAsync(_app, group, group.OwnerRowId, group.ThirdRowId);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.ThirdRowId, group.OwnerRowId);
            await EndedMemberScenario.EndBojanMembershipAsync(group, endKind);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.NotNull(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Equal([30_000L, 0L, 0L, 0L, -30_000L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }
    }
}
