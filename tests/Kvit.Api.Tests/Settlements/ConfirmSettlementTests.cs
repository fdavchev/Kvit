using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class ConfirmSettlementTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task Confirm_ByTheReceiverWhoIsNotTheOwner_Answers204WithNoBody()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Third.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Confirm_ByTheOwnerWhoIsTheReceiver_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Confirm_ByTheReceiverOfASettlementRecordedByTheOwnerForAPlainNamePayer_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Member.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Confirm_ByTheOwnerForAPlainNameReceiver_Answers204AndRecordsTheOwnerAsTheResolver()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "resolved_by_user_id"));
        }

        [Fact]
        public async Task Confirm_ByTheOwnerWhoIsThePayerToAPlainNameReceiver_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingAnaToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
        }

        [Fact]
        public async Task Confirm_SetsConfirmedWithTheConfirmerAndTheClockTimeAndKeepsTheRestOfTheRow()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            string createdAtBefore = (await SettlementRows.ValueAsync(_app, settlementId, "created_at"))!;
            _app.Clock.Advance(OneMinute);

            await SettlementSteps.ConfirmAsync(group.Third, group.GroupId, settlementId);

            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(group.Third.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "resolved_by_user_id"));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await SettlementRows.ValueAsync(_app, settlementId, "resolved_at AT TIME ZONE 'UTC'"));
            Assert.Equal(group.Member.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "recorded_by_user_id"));
            Assert.Equal(createdAtBefore, await SettlementRows.ValueAsync(_app, settlementId, "created_at"));
            Assert.Equal("30000", await SettlementRows.ValueAsync(_app, settlementId, "amount_minor"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
        }

        [Fact]
        public async Task Confirm_MovesTheBalancesAndTheyStillSumToZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);

            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Confirm_ByThePayer_Answers403SettlementNotAllowedAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_ByAnUnrelatedMember_Answers403SettlementNotAllowedAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Third.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_ByTheOwnerWhenTheReceiverHasAnAccountAndIsSomeoneElse_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_ByANonOwnerForAPlainNameReceiver_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Third.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_SettlementThatWasConfirmedAtOnce_Answers400NotPendingAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_RejectedSettlement_Answers400NotPendingAndKeepsItRejected()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal("Rejected", await SettlementRows.ValueAsync(_app, settlementId, "status"));
        }

        [Fact]
        public async Task Confirm_CancelledSettlement_Answers400NotPendingAndKeepsItCancelled()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal("Cancelled", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Confirm_Twice_SecondAnswers400NotPendingAndWritesOneConfirmedEventAndOneUsageEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);
            string resolvedAt = (await SettlementRows.ValueAsync(_app, settlementId, "resolved_at"))!;
            _app.Clock.Advance(OneMinute);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal(resolvedAt, await SettlementRows.ValueAsync(_app, settlementId, "resolved_at"));
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementConfirmed"));
            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Confirm_SettlementThatDoesNotExist_Answers404SettlementNotFound()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
        }

        [Fact]
        public async Task Confirm_SettlementOfAnotherGroupOfTheSameCaller_Answers404SettlementNotFoundAndKeepsItPending()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SettlementGroup other = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementInOther = await SettlementSteps.PendingBojanToAnaAsync(other);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementInOther);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
            Assert.Equal("Pending", await SettlementRows.ValueAsync(_app, settlementInOther, "status"));
        }
    }
}
