using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class LeaveGroupZeroBalanceTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(ExpenseInputs.Mkd)]
        [InlineData(ExpenseInputs.Eur)]
        public async Task Leave_MemberWhoOwesMoney_Answers400MemberBalanceNotZeroAndWritesNothing(string currency)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, currency);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberLeft"));
            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(group.Member.Client, group.GroupId)).StatusCode);
            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, currency));
        }

        [Fact]
        public async Task Leave_MemberWhoIsOwedMoney_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AnaOwesBojanAsync(_app, group);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Leave_MemberWhoIsZeroInMkdButOwesInEur_Answers400MemberBalanceNotZeroAndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, ExpenseInputs.Mkd);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group, ExpenseInputs.Mkd);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, ExpenseInputs.Eur);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal([0L, 0L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Eur));
        }

        [Fact]
        public async Task Leave_MemberWhoOverpaidTheirDebt_Answers400BecauseTheyAreNowOwed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 50_000);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal([-20_000L, 20_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Leave_MemberWhoseBalanceComesOnlyFromAConfirmedPayment_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Leave_MemberWithAPendingPaymentTheyMadeAndZeroBalance_Answers400MemberHasPendingPaymentAndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberLeft"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Leave_MemberWithAPendingPaymentOwedToThemAndZeroBalance_Answers400MemberHasPendingPayment()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AnaPaysBojanWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Leave_MemberWithAPendingPaymentInEurAndZeroBalances_Answers400MemberHasPendingPayment()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Eur);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
        }

        [Fact]
        public async Task Leave_MemberWithBothANonZeroBalanceAndAPendingPayment_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
        }

        [Fact]
        public async Task Leave_PendingPaymentThatSettlesTheDebt_IsRefusedUntilTheReceiverConfirmsAndThenAnswers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            HttpResponseMessage refused = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Left", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "end_kind"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberLeft"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Leave_PendingPaymentWithZeroBalance_IsRefusedUntilTheReceiverRejectsAndThenAnswers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            HttpResponseMessage refused = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberLeft"));
        }

        [Fact]
        public async Task Leave_PendingPaymentWithZeroBalance_IsRefusedUntilThePayerCancelsAndThenAnswers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.AnaPaysBojanWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            HttpResponseMessage refused = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.MEMBER_HAS_PENDING_PAYMENT);
            await SettlementSteps.CancelAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData(ZeroBalanceScenario.RejectedStatus)]
        [InlineData(ZeroBalanceScenario.CancelledStatus)]
        public async Task Leave_MemberWhoseOnlyPaymentWasRejectedOrCancelledAsPayer_Answers204(string status)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, status);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Theory]
        [InlineData(ZeroBalanceScenario.RejectedStatus)]
        [InlineData(ZeroBalanceScenario.CancelledStatus)]
        public async Task Leave_MemberWhoseOnlyPaymentWasRejectedOrCancelledAsReceiver_Answers204(string status)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AnaPaysBojanWithStatusAsync(group, status);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Leave_MemberWhoseDebtHasARejectedPayment_StillAnswers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.RejectedStatus);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
        }

        [Fact]
        public async Task Leave_MemberWhoseOnlyPaymentWasDeleted_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Leave_MemberWhoseOnlyPendingPaymentRowIsDeleted_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementTableRows.InsertSettlementAsync(_app, group.GroupId, group.MemberRowId, group.OwnerRowId, group.Member.UserId, deletedAt: DateTimeOffset.UtcNow);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Leave_AfterRestoringADeletedPaymentThatMakesTheMemberNonZero_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
            await SettlementSteps.RestoreAsync(group.Owner, group.GroupId, settlementId);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Leave_DebtSettledExactlyByAConfirmedPayment_Answers204AndWritesOneMemberLeftEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Left", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "end_kind"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "MemberLeft"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Leave_DebtsInBothCurrenciesSettledExactly_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, ExpenseInputs.Mkd);
            await ZeroBalanceScenario.AnaOwesBojanAsync(_app, group, ExpenseInputs.Eur);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group, ExpenseInputs.Mkd);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Eur);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Leave_MemberWhoseExpensesNetToExactlyZero_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.AnaOwesBojanAsync(_app, group);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Leave_MemberWhoseOnlyExpenseWasDeleted_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid expenseId = await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Leave_MemberWhoIsNotInvolvedWhileOthersOweAndHavePendingPayments_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AddTwoPersonExpenseAsync(_app, group, group.OwnerRowId, group.ThirdRowId);
            await SettlementSteps.RecordAsync(group.Third, group.GroupId, group.ThirdRowId, group.OwnerRowId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal([30_000L, 0L, 0L, 0L, -30_000L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Leave_MemberWhoClaimedANameThatOwesMoney_Answers400MemberBalanceNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            HttpResponseMessage claim = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);
            Assert.Equal(HttpStatusCode.NoContent, claim.StatusCode);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_BALANCE_NOT_ZERO);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Leave_OwnerWithANonZeroBalance_Answers400OwnerCannotLeaveBeforeLookingAtBalances()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_OWNER_CANNOT_LEAVE);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Leave_NonMemberOfAGroupWithDebts_Answers404GroupNotFound()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(stranger.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }
    }
}
