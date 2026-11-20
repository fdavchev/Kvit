using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class SettlementChangedMeanwhileTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string ConfirmedEvent = "SettlementConfirmed";
        private const string CancelledEvent = "SettlementCancelled";
        private const string DeletedEvent = "SettlementDeleted";
        private const string RestoredEvent = "SettlementRestored";
        private const string RejectedEvent = "SettlementRejected";

        [Fact]
        public async Task ConfirmAndCancel_AtTheSameMoment_OneAnswers204AndTheOtherAnswers400NotPendingWithOneActivityLine()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await using InsertGate gate = await HoldAtAsync(ConfirmedEvent, CancelledEvent);

            Task<HttpResponseMessage> confirm = SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);
            Task<HttpResponseMessage> cancel = SettlementRequests.CancelAsync(group.Member.Client, group.GroupId, settlementId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(confirm, cancel);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.BadRequest], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.BadRequest), HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            bool confirmWon = responses[0].StatusCode == HttpStatusCode.NoContent;
            Assert.Equal(confirmWon ? "Confirmed" : "Cancelled", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(confirmWon ? 1 : 0, await SettlementRows.CountEventsAsync(_app, settlementId, ConfirmedEvent));
            Assert.Equal(confirmWon ? 0 : 1, await SettlementRows.CountEventsAsync(_app, settlementId, CancelledEvent));
            Assert.Equal(confirmWon ? 1 : 0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Delete_TwoDeletesAtTheSameMoment_OneAnswers204AndTheOtherAnswers404NotFoundWithOneActivityLine()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            await using InsertGate gate = await HoldAtAsync(DeletedEvent);

            Task<HttpResponseMessage> first = SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);
            Task<HttpResponseMessage> second = SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NotFound], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.NotFound), HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, DeletedEvent));
        }

        [Fact]
        public async Task Restore_TwoRestoresAtTheSameMoment_OneAnswers204AndTheOtherAnswers400NotDeletedWithOneActivityLine()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await using InsertGate gate = await HoldAtAsync(RestoredEvent);

            Task<HttpResponseMessage> first = SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);
            Task<HttpResponseMessage> second = SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.BadRequest], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.BadRequest), HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_DELETED);
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, RestoredEvent));
        }

        [Fact]
        public async Task ConfirmAndReject_ByTheReceiverAtTheSameMoment_OneAnswers204AndTheOtherAnswers400NotPendingAndTheBalancesMatchTheWinner()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            await using InsertGate gate = await HoldAtAsync(ConfirmedEvent, RejectedEvent);

            Task<HttpResponseMessage> confirm = SettlementRequests.ConfirmAsync(group.Third.Client, group.GroupId, settlementId);
            Task<HttpResponseMessage> reject = SettlementRequests.RejectAsync(group.Third.Client, group.GroupId, settlementId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(confirm, reject);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.BadRequest], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.BadRequest), HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            bool confirmWon = responses[0].StatusCode == HttpStatusCode.NoContent;
            Assert.Equal(confirmWon ? "Confirmed" : "Rejected", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(confirmWon ? 1 : 0, await SettlementRows.CountEventsAsync(_app, settlementId, ConfirmedEvent));
            Assert.Equal(confirmWon ? 0 : 1, await SettlementRows.CountEventsAsync(_app, settlementId, RejectedEvent));
            Assert.Equal(confirmWon ? 1 : 0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Third.UserId));
            await AssertBalancesAsync(group, confirmWon);
        }

        [Fact]
        public async Task RejectAndCancel_AtTheSameMoment_OneAnswers204AndTheOtherAnswers400NotPendingAndNothingMovesTheBalances()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            await using InsertGate gate = await HoldAtAsync(RejectedEvent, CancelledEvent);

            Task<HttpResponseMessage> reject = SettlementRequests.RejectAsync(group.Third.Client, group.GroupId, settlementId);
            Task<HttpResponseMessage> cancel = SettlementRequests.CancelAsync(group.Member.Client, group.GroupId, settlementId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(reject, cancel);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.BadRequest], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.BadRequest), HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            bool rejectWon = responses[0].StatusCode == HttpStatusCode.NoContent;
            Assert.Equal(rejectWon ? "Rejected" : "Cancelled", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(rejectWon ? 1 : 0, await SettlementRows.CountEventsAsync(_app, settlementId, RejectedEvent));
            Assert.Equal(rejectWon ? 0 : 1, await SettlementRows.CountEventsAsync(_app, settlementId, CancelledEvent));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Confirm_TwoConfirmsByTheReceiverAtTheSameMoment_OneAnswers204AndTheOtherAnswers400NotPendingWithOneActivityLineAndOneUsageEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            await using InsertGate gate = await HoldAtAsync(ConfirmedEvent);

            Task<HttpResponseMessage> first = SettlementRequests.ConfirmAsync(group.Third.Client, group.GroupId, settlementId);
            Task<HttpResponseMessage> second = SettlementRequests.ConfirmAsync(group.Third.Client, group.GroupId, settlementId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.BadRequest], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.BadRequest), HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, ConfirmedEvent));
            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Third.UserId));
            await AssertBalancesAsync(group, true);
        }

        [Fact]
        public async Task Reject_TwoRejectsByTheReceiverAtTheSameMoment_OneAnswers204AndTheOtherAnswers400NotPendingWithOneActivityLine()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            await using InsertGate gate = await HoldAtAsync(RejectedEvent);

            Task<HttpResponseMessage> first = SettlementRequests.RejectAsync(group.Third.Client, group.GroupId, settlementId);
            Task<HttpResponseMessage> second = SettlementRequests.RejectAsync(group.Third.Client, group.GroupId, settlementId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.BadRequest], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.BadRequest), HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_PENDING);
            Assert.Equal("Rejected", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, RejectedEvent));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Delete_WhileTheOtherPersonDeletesAndRestoresTheSamePaymentMeanwhile_RunsAgainAndDeletesItWithAllHistoryLines()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);
            await using InsertGate gate = await HoldAtByAsync(DeletedEvent, group.Member.UserId);

            Task<HttpResponseMessage> slowDelete = SettlementRequests.DeleteAsync(group.Member.Client, group.GroupId, settlementId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage deleted = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);
            HttpResponseMessage restored = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);
            await gate.OpenAsync();
            HttpResponseMessage slowResult = await slowDelete;

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, slowResult.StatusCode);
            Assert.Equal("true", await SettlementRows.ValueAsync(_app, settlementId, "deleted_at IS NOT NULL"));
            Assert.Equal(2, await SettlementRows.CountEventsAsync(_app, settlementId, DeletedEvent));
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, RestoredEvent));
        }

        [Fact]
        public async Task Restore_WhileTheOtherPersonRestoresAndDeletesTheSamePaymentAgainMeanwhile_RunsAgainAndRestoresIt()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await using InsertGate gate = await HoldAtByAsync(RestoredEvent, group.Member.UserId);

            Task<HttpResponseMessage> slowRestore = SettlementRequests.RestoreAsync(group.Member.Client, group.GroupId, settlementId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage restored = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);
            HttpResponseMessage deletedAgain = await SettlementRequests.DeleteAsync(group.Owner.Client, group.GroupId, settlementId);
            await gate.OpenAsync();
            HttpResponseMessage slowResult = await slowRestore;

            Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, deletedAgain.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, slowResult.StatusCode);
            Assert.Equal("false", await SettlementRows.ValueAsync(_app, settlementId, "deleted_at IS NOT NULL"));
            Assert.Equal(2, await SettlementRows.CountEventsAsync(_app, settlementId, RestoredEvent));
            Assert.Equal(2, await SettlementRows.CountEventsAsync(_app, settlementId, DeletedEvent));
            await AssertBalancesAsync(group, true);
        }

        private static async Task AssertBalancesAsync(SettlementGroup group, bool settlementCounts)
        {
            if (!settlementCounts)
            {
                await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
                return;
            }

            List<long> balances = [.. (await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD")).Order()];
            Assert.Equal([-30_000L, 0L, 0L, 0L, 30_000L], balances);
        }

        private Task<InsertGate> HoldAtByAsync(string eventType, Guid actorUserId)
        {
            return InsertGate.CloseAsync(_app, "activity_events", $"NEW.type = '{eventType}' AND NEW.actor_user_id = '{actorUserId}'");
        }

        private Task<InsertGate> HoldAtAsync(params string[] eventTypes)
        {
            string typeList = string.Join(", ", eventTypes.Select(eventType => $"'{eventType}'"));

            return InsertGate.CloseAsync(_app, "activity_events", $"NEW.type IN ({typeList})");
        }
    }
}
