using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class DeleteSettlementTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task Delete_ByWhoeverRecordedIt_Answers204WithNoBody()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Member.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Delete_ByTheOwnerOfASettlementRecordedByAnotherMember_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "deleted_by_user_id"));
        }

        [Fact]
        public async Task Delete_ByThePayerWhoRecordedAPendingOneThatTheReceiverThenConfirmed_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            await SettlementSteps.ConfirmAsync(group.Third, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Member.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ByTheReceiverWhoConfirmedAPendingOneButDidNotRecordItAndIsNotTheOwner_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            await SettlementSteps.ConfirmAsync(group.Third, group.GroupId, settlementId);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Third.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Delete_ByAnUnrelatedMember_Answers403SettlementNotAllowedAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Third.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Delete_ByThePayerOfASettlementThatTheReceiverRecordedAtOnce_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.ThirdRowId, group.MemberRowId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Third.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
        }

        [Fact]
        public async Task Delete_SetsDeletedAtFromTheClockAndDeletedByAndKeepsTheRowAsConfirmed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            _app.Clock.Advance(OneMinute);

            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await SettlementRows.ValueAsync(_app, settlementId, "deleted_at AT TIME ZONE 'UTC'"));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "deleted_by_user_id"));
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Delete_TakesTheSettlementOutOfTheBalancesAndTheyStillSumToZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(60_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000);
            Assert.Equal([20_000L, -20_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));

            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Delete_WritesNoUsageEventBeyondTheConfirmedOne()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);

            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Delete_PendingSettlement_Answers400NotConfirmedAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_CONFIRMED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Delete_PendingSettlementByTheOwner_Answers400NotConfirmed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_CONFIRMED);
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
        }

        [Fact]
        public async Task Delete_RejectedSettlement_Answers400NotConfirmedAndKeepsItRejected()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_CONFIRMED);
            Assert.Equal("Rejected", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
        }

        [Fact]
        public async Task Delete_CancelledSettlement_Answers400NotConfirmedAndKeepsItCancelled()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_CONFIRMED);
            Assert.Equal("Cancelled", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
        }

        [Fact]
        public async Task Delete_AlreadyDeletedSettlement_Answers404SettlementNotFoundAndWritesNoSecondEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementDeleted"));
        }

        [Fact]
        public async Task Delete_SettlementThatDoesNotExist_Answers404SettlementNotFound()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
        }

        [Fact]
        public async Task Delete_SettlementOfAnotherGroup_Answers404SettlementNotFoundAndKeepsItConfirmed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SettlementGroup other = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementInOther = await SettlementSteps.ConfirmedBojanToAnaAsync(other);

            HttpResponseMessage response = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementInOther);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementInOther, "deleted_at"));
        }
    }
}
