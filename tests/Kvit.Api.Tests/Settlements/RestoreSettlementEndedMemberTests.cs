using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RestoreSettlementEndedMemberTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(PaymentSide.Payer, EndedMemberScenario.Left)]
        [InlineData(PaymentSide.Payer, EndedMemberScenario.Removed)]
        [InlineData(PaymentSide.Receiver, EndedMemberScenario.Left)]
        [InlineData(PaymentSide.Receiver, EndedMemberScenario.Removed)]
        public async Task Restore_PaymentOfAMemberWhoHasLeftOrWasRemovedSinceTheDelete_Answers400MemberNotCurrentAndChangesNothing(PaymentSide bojanIs, string endKind)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await EndedMemberScenario.RecordConfirmedPaymentAsync(group, bojanIs);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await EndedMemberScenario.EndBojanMembershipAsync(group, endKind);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.NotNull(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "deleted_by_user_id"));
            Assert.Equal(0, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementRestored"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Restore_PaymentOfAPlainNameWhoWasRemovedSinceTheDelete_Answers400MemberNotCurrentAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await EndedMemberScenario.RemoveMarkoAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.NotNull(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Restore_PaymentWhenBothMembersAreCurrentAndBojanPaid_Answers204AndTakesTheMoneyBack()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await EndedMemberScenario.RecordConfirmedPaymentAsync(group, PaymentSide.Payer);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Restore_PaymentWhenBothMembersAreCurrentAndBojanWasPaid_Answers204AndTakesTheMoneyBack()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await EndedMemberScenario.RecordConfirmedPaymentAsync(group, PaymentSide.Receiver);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Restore_PaymentOfAPlainNameWhoIsStillAMember_Answers204AndTakesTheMoneyBack()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Equal([-30_000L, 0L, 30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Theory]
        [InlineData(EndedMemberScenario.Left)]
        [InlineData(EndedMemberScenario.Removed)]
        public async Task Restore_PaymentBetweenTwoCurrentMembersWhileAnotherMemberHasEnded_Answers204(string endKind)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.ThirdRowId, group.OwnerRowId);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await EndedMemberScenario.EndBojanMembershipAsync(group, endKind);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Equal([-30_000L, 0L, 0L, 0L, 30_000L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }
    }
}
