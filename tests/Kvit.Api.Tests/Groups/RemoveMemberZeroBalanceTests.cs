using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class RemoveMemberZeroBalanceTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(ExpenseInputs.Mkd)]
        [InlineData(ExpenseInputs.Eur)]
        public async Task Remove_PlainNameWhoOwesMoney_Answers400MemberBalanceNotZeroAndWritesNothing(string currency)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group, currency);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberRemoved"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.MarkoRowId, "removed_at"));
            Assert.Equal([30_000L, 0L, -30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, currency));
        }

        [Fact]
        public async Task Remove_PlainNameWhoIsOwedMoney_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AddTwoPersonExpenseAsync(_app, group, group.MarkoRowId, group.OwnerRowId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal([-30_000L, 0L, 30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Theory]
        [InlineData(ExpenseInputs.Mkd)]
        [InlineData(ExpenseInputs.Eur)]
        public async Task Remove_AccountMemberWhoOwesMoney_Answers400MemberBalanceNotZeroAndKeepsThemInTheGroup(string currency)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, currency);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(group.Member.Client, group.GroupId)).StatusCode);
            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, currency));
        }

        [Fact]
        public async Task Remove_AccountMemberWhoIsOwedMoney_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AnaOwesBojanAsync(_app, group);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Remove_MemberWhoIsZeroInMkdButOwesInEur_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group, ExpenseInputs.Mkd);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Mkd);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group, ExpenseInputs.Eur);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal([0L, 0L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
            Assert.Equal([30_000L, 0L, -30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Eur));
        }

        [Fact]
        public async Task Remove_PlainNameWhoseBalanceComesOnlyFromAConfirmedPayment_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal([-30_000L, 0L, 30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Remove_MemberWhoOverpaidTheirDebt_Answers400BecauseTheyAreNowOwed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId, 50_000);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal([-20_000L, 0L, 20_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Remove_PlainNameWithAPendingPaymentAsPayerAndZeroBalance_Answers400MemberHasPendingPaymentAndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberRemoved"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Remove_AccountMemberWhoIsTheReceiverOfAPendingPaymentAndZeroBalance_Answers400MemberHasPendingPayment()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
        }

        [Fact]
        public async Task Remove_AccountMemberWhoIsThePayerOfAPendingPaymentAndZeroBalance_Answers400MemberHasPendingPayment()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
        }

        [Fact]
        public async Task Remove_AccountMemberWhoIsTheReceiverOfAPendingPaymentFromAnAccount_Answers400MemberHasPendingPayment()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AnaPaysBojanWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
        }

        [Fact]
        public async Task Remove_MemberWithAPendingPaymentInEurAndZeroBalances_Answers400MemberHasPendingPayment()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Eur);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
        }

        [Fact]
        public async Task Remove_MemberWithBothANonZeroBalanceAndAPendingPayment_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
        }

        [Fact]
        public async Task Remove_PendingPaymentThatSettlesTheDebt_IsRefusedUntilTheReceiverConfirmsAndThenAnswers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            HttpResponseMessage refused = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Removed", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "end_kind"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberRemoved"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Remove_PlainNameWithAPendingPayment_IsRefusedUntilTheReceiverRejectsAndThenAnswers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);
            HttpResponseMessage refused = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
            await SettlementSteps.RejectAsync(group.Member, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberRemoved"));
        }

        [Theory]
        [InlineData(ZeroBalanceScenario.RejectedStatus)]
        [InlineData(ZeroBalanceScenario.CancelledStatus)]
        public async Task Remove_MemberWhoseOnlyPaymentWasRejectedOrCancelledAsPayer_Answers204(string status)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, status);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Theory]
        [InlineData(ZeroBalanceScenario.RejectedStatus)]
        [InlineData(ZeroBalanceScenario.CancelledStatus)]
        public async Task Remove_MemberWhoseOnlyPaymentWasRejectedOrCancelledAsReceiver_Answers204(string status)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AnaPaysBojanWithStatusAsync(group, status);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Remove_MemberWhoseOnlyPaymentWasDeleted_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Remove_MemberWhoseOnlyPendingPaymentRowIsDeleted_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementTableRows.InsertSettlementAsync(_app, group.GroupId, group.MarkoRowId, group.MemberRowId, group.Owner.UserId, deletedAt: DateTimeOffset.UtcNow);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Remove_AfterRestoringADeletedPaymentThatMakesTheMemberNonZero_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await SettlementSteps.RestoreAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal([-30_000L, 0L, 30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Remove_AccountMemberWhoseDebtIsSettledExactly_Answers204AndWritesOneMemberRemovedEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Removed", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "end_kind"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberRemoved"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Remove_PlainNameWhoseDebtIsSettledExactlyByTheOwner_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Remove_MemberWhoseDebtsInBothCurrenciesAreSettledExactly_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group, ExpenseInputs.Mkd);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group, ExpenseInputs.Eur);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Mkd);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Eur);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Remove_MemberWhoseExpensesNetToExactlyZero_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.AnaOwesBojanAsync(_app, group);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Remove_PlainNameWhoseOnlyExpenseWasDeleted_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid expenseId = await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Remove_MemberWhoIsNotInvolvedWhileOthersOweAndHavePendingPayments_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            await SettlementSteps.RecordAsync(group.Third, group.GroupId, group.ThirdRowId, group.OwnerRowId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.CvetaRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal([30_000L, 0L, -30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Remove_MemberWhoClaimedANameThatOwesMoney_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            HttpResponseMessage claim = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);
            Assert.Equal(HttpStatusCode.NoContent, claim.StatusCode);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
        }

        [Fact]
        public async Task Remove_MemberWhoIsNotTheOwnerTargetingSomeoneWithADebt_Answers403GroupNotOwnerAndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Remove_NonMemberOfAGroupWithDebts_Answers404GroupNotFound()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(stranger.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Remove_OwnerRemovesThemselfWithANonZeroBalance_Answers400MemberIsOwnerBeforeLookingAtBalances()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.OwnerRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_IS_OWNER);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Remove_UnknownMemberIdInAGroupWithDebts_Answers404MemberNotFound()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Remove_RowThatAlreadyLeftWithADebt_Answers404MemberNotFoundAndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Left");
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }
    }
}
