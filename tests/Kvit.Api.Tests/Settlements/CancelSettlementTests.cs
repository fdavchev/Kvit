using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class CancelSettlementTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task Cancel_ByThePayer_Answers204WithNoBody()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Member.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Cancel_ByTheOwnerWhoIsThePayer_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingAnaToBojanAsync(group);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Owner.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Cancel_ByTheOwnerForAPlainNamePayer_Answers204AndRecordsTheOwnerAsTheResolver()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Owner.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "resolved_by_user_id"));
        }

        [Fact]
        public async Task Cancel_SetsCancelledWithTheCancellerAndTheClockTimeAndTheBalancesDoNotMove()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            _app.Clock.Advance(OneMinute);

            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            Assert.Equal("Cancelled", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(group.Member.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "resolved_by_user_id"));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await SettlementRows.ValueAsync(_app, settlementId, "resolved_at AT TIME ZONE 'UTC'"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Cancel_WritesNoConfirmedUsageEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Member.UserId));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Cancel_ByTheReceiver_Answers403SettlementNotAllowedAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Cancel_ByAnUnrelatedMember_Answers403SettlementNotAllowedAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Third.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Cancel_ByTheOwnerWhenThePayerHasAnAccountAndIsSomeoneElse_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Cancel_ByANonOwnerForAPlainNamePayer_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Third.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
        }

        [Fact]
        public async Task Cancel_ByTheReceiverOfAPlainNamePayersSettlement_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
        }

        [Fact]
        public async Task Cancel_ConfirmedSettlement_Answers400NotPendingAndKeepsItConfirmed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Cancel_RejectedSettlement_Answers400NotPending()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal("Rejected", await SettlementRows.ValueAsync(_app, settlementId, "status"));
        }

        [Fact]
        public async Task Cancel_SettlementRecordedByTheReceiverAndConfirmedAtOnce_Answers400NotPending()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
        }

        [Fact]
        public async Task Cancel_Twice_SecondAnswers400NotPendingAndWritesOneCancelledEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementCancelled"));
        }

        [Fact]
        public async Task Cancel_SettlementThatDoesNotExist_Answers404SettlementNotFound()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Member.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
        }

        [Fact]
        public async Task Cancel_SettlementOfAnotherGroupOfTheSameCaller_Answers404SettlementNotFoundAndKeepsItPending()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SettlementGroup other = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementInOther = await SettlementSteps.PendingBojanToAnaAsync(other);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Owner.Client, group.GroupId, settlementInOther);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
            Assert.Equal("Pending", await SettlementRows.ValueAsync(_app, settlementInOther, "status"));
        }
    }
}
