using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class DeleteGroupZeroBalanceTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(ExpenseInputs.Mkd)]
        [InlineData(ExpenseInputs.Eur)]
        public async Task Delete_GroupWhereSomeoneOwesMoney_Answers400GroupBalancesNotZeroAndWritesNothing(string currency)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, currency);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "deleted_at"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupDeleted"));
            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(group.Owner.Client, group.GroupId)).StatusCode);
            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, currency));
        }

        [Fact]
        public async Task Delete_GroupWhereOnlyPlainNamesHaveBalances_Answers400GroupBalancesNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AddTwoPersonExpenseAsync(_app, group, group.MarkoRowId, group.CvetaRowId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            Assert.Equal([0L, 0L, 30_000L, -30_000L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Delete_GroupWhereMkdIsSettledButEurIsNot_Answers400GroupBalancesNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, ExpenseInputs.Mkd);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group, ExpenseInputs.Mkd);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, ExpenseInputs.Eur);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            Assert.Equal([0L, 0L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Eur));
        }

        [Fact]
        public async Task Delete_GroupWhereOnlyAConfirmedPaymentLeavesBalances_Answers400GroupBalancesNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
        }

        [Fact]
        public async Task Delete_GroupWhereTheDebtWasOverpaid_Answers400GroupBalancesNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 50_000);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            Assert.Equal([-20_000L, 20_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        public async Task Delete_GroupWhereAFormerMemberStillHasABalance_Answers400GroupBalancesNotZero(string endKind)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }

        [Fact]
        public async Task Delete_GroupWithAPendingPaymentAndEveryBalanceZero_Answers400GroupHasPendingPaymentAndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_HAS_PENDING_PAYMENT);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "deleted_at"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupDeleted"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Delete_GroupWithAPendingPaymentOfAPlainName_Answers400GroupHasPendingPayment()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_HAS_PENDING_PAYMENT);
        }

        [Fact]
        public async Task Delete_GroupWithAPendingPaymentBetweenTwoOtherPeopleInEur_Answers400GroupHasPendingPayment()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Third, group.GroupId, group.ThirdRowId, group.MemberRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Eur);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_HAS_PENDING_PAYMENT);
        }

        [Fact]
        public async Task Delete_GroupWithBothANonZeroBalanceAndAPendingPayment_Answers400GroupBalancesNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
        }

        [Fact]
        public async Task Delete_PendingPaymentThatSettlesTheDebt_IsRefusedUntilTheReceiverConfirmsAndThenAnswers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            HttpResponseMessage refused = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.NotNull(await GroupRows.GroupValueAsync(_app, group.GroupId, "deleted_at"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupDeleted"));
        }

        [Fact]
        public async Task Delete_PendingPaymentWithZeroBalances_IsRefusedUntilTheReceiverRejectsAndThenAnswers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            HttpResponseMessage refused = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.GROUP_HAS_PENDING_PAYMENT);
            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_PendingPaymentWithZeroBalances_IsRefusedUntilThePayerCancelsAndThenAnswers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            HttpResponseMessage refused = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.GROUP_HAS_PENDING_PAYMENT);
            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData(ZeroBalanceScenario.RejectedStatus)]
        [InlineData(ZeroBalanceScenario.CancelledStatus)]
        public async Task Delete_GroupWhoseOnlyPaymentWasRejectedOrCancelled_Answers204(string status)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, status);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_GroupWhoseOnlyPaymentWasDeleted_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_GroupWhoseOnlyPendingPaymentRowIsDeleted_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementTableRows.InsertSettlementAsync(_app, group.GroupId, group.MemberRowId, group.OwnerRowId, group.Member.UserId, deletedAt: DateTimeOffset.UtcNow);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_AfterRestoringADeletedPaymentThatMakesSomeoneNonZero_Answers400GroupBalancesNotZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await SettlementSteps.RestoreAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_BALANCES_NOT_ZERO);
        }

        [Fact]
        public async Task Delete_GroupWhereEveryDebtInBothCurrenciesIsSettledExactly_Answers204AndMovesToRecentlyDeleted()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, ExpenseInputs.Mkd);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group, ExpenseInputs.Eur);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group, ExpenseInputs.Mkd);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Eur);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.NotNull(await GroupRows.GroupValueAsync(_app, group.GroupId, "deleted_at"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupDeleted"));
            JsonElement list = await AuthRequests.ReadJsonAsync(await GroupRequests.ListAsync(group.Owner.Client));
            Assert.Contains(group.GroupId, GroupRequests.IdsOf(list.GetProperty("recentlyDeleted")));
        }

        [Fact]
        public async Task Delete_GroupWhoseExpensesNetToExactlyZero_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.AnaOwesBojanAsync(_app, group);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_GroupWhoseOnlyExpenseWasDeleted_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid expenseId = await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_GroupWhileAnotherGroupHasDebtsAndPendingPayments_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SettlementGroup otherGroup = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, otherGroup);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(otherGroup, ZeroBalanceScenario.PendingStatus);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await GroupRows.GroupValueAsync(_app, otherGroup.GroupId, "deleted_at"));
        }

        [Fact]
        public async Task Delete_MemberWhoIsNotTheOwnerOfAGroupWithDebts_Answers403GroupNotOwnerAndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Delete_NonMemberOfAGroupWithDebts_Answers404GroupNotFound()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.DeleteAsync(stranger.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Restore_GroupDeletedWithAllDebtsSettled_StillAnswers204AndBringsTheHistoryBack()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);
            await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            HttpResponseMessage response = await GroupRequests.RestoreAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "deleted_at"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupRestored"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Restore_GroupThatWasRefusedDeletion_Answers400GroupNotDeleted()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            HttpResponseMessage response = await GroupRequests.RestoreAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_DELETED);
        }

        [Fact]
        public async Task Restore_GroupDeletedWhileBalancesAreNotZero_DoesNotCheckBalancesAndAnswers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 1);

            HttpResponseMessage response = await GroupRequests.RestoreAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, ExpenseInputs.Mkd));
        }
    }
}
