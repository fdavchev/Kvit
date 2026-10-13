using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RejectSettlementTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task Reject_ByTheReceiverWhoIsNotTheOwner_Answers204WithNoBody()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Third.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Reject_ByTheOwnerForAPlainNameReceiver_Answers204AndRecordsTheOwnerAsTheResolver()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Owner.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "resolved_by_user_id"));
        }

        [Fact]
        public async Task Reject_ByTheReceiverOfASettlementRecordedByTheOwnerForAPlainNamePayer_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Member.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Reject_SetsRejectedWithTheRejecterAndTheClockTimeAndTheBalancesDoNotMove()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            _app.Clock.Advance(OneMinute);

            await SettlementSteps.RejectAsync(group.Third, group.GroupId, settlementId);

            Assert.Equal("Rejected", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(group.Third.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "resolved_by_user_id"));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await SettlementRows.ValueAsync(_app, settlementId, "resolved_at AT TIME ZONE 'UTC'"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Reject_WritesNoConfirmedUsageEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, settlementId);

            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Reject_ByThePayer_Answers403SettlementNotAllowedAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Reject_ByAnUnrelatedMember_Answers403SettlementNotAllowedAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Third.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Reject_ByTheOwnerWhenTheReceiverHasAnAccountAndIsSomeoneElse_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
        }

        [Fact]
        public async Task Reject_ByANonOwnerForAPlainNameReceiver_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Third.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
        }

        [Fact]
        public async Task Reject_ConfirmedSettlement_Answers400NotPendingAndKeepsItConfirmed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Reject_CancelledSettlement_Answers400NotPending()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal("Cancelled", await SettlementRows.ValueAsync(_app, settlementId, "status"));
        }

        [Fact]
        public async Task Reject_Twice_SecondAnswers400NotPendingAndWritesOneRejectedEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementRejected"));
        }

        [Fact]
        public async Task Reject_SettlementThatDoesNotExist_Answers404SettlementNotFound()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
        }

        [Fact]
        public async Task Reject_SettlementOfAnotherGroupOfTheSameCaller_Answers404SettlementNotFoundAndKeepsItPending()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SettlementGroup other = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementInOther = await SettlementSteps.PendingBojanToAnaAsync(other);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Owner.Client, group.GroupId, settlementInOther);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
            Assert.Equal("Pending", await SettlementRows.ValueAsync(_app, settlementInOther, "status"));
        }
    }
}
