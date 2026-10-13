using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RecordSettlementConcurrentDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Repetitions = 20;

        [Fact]
        public async Task Record_SameClientRequestIdSentTwiceAtTheSameMoment_Answers200BothTimesWithTheSameSettlement()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                Guid clientRequestId = Guid.CreateVersion7();

                HttpResponseMessage[] responses = await Task.WhenAll(
                    SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId),
                    SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId));

                Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
                Guid first = (await AuthRequests.ReadJsonAsync(responses[0])).GetProperty("id").GetGuid();
                Guid second = (await AuthRequests.ReadJsonAsync(responses[1])).GetProperty("id").GetGuid();
                Assert.Equal(first, second);
            }

            Assert.Equal(Repetitions, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal(Repetitions, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "SettlementRecorded"));
            Assert.Equal(Repetitions, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Record_SecondRequestWithTheSameClientRequestIdArrivesWhileTheFirstIsStillSaving_Answers200BothTimesWithTheSameSettlement()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            await using SettlementInsertGate gate = await SettlementInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> first = SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Guid firstId = (await AuthRequests.ReadJsonAsync(responses[0])).GetProperty("id").GetGuid();
            Guid secondId = (await AuthRequests.ReadJsonAsync(responses[1])).GetProperty("id").GetGuid();
            Assert.Equal(firstId, secondId);
        }

        [Fact]
        public async Task Record_SecondRequestWithTheSameClientRequestIdArrivesWhileTheFirstIsStillSaving_WritesOneRowOneEventAndOneUsageEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            await using SettlementInsertGate gate = await SettlementInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> first = SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            await Task.WhenAll(first, second);

            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "SettlementRecorded"));
            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Record_PendingSettlementSentTwiceWhileTheFirstIsStillSaving_WritesOnePendingRowAndNoUsageEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            await using SettlementInsertGate gate = await SettlementInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> first = SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "SettlementRecorded"));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Record_SameClientRequestIdFromAnotherPersonOfTheSameGroupAtTheSameMoment_AnswersOne200AndOne400AndSavesOneRow()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            await using SettlementInsertGate gate = await SettlementInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> first = SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = SettlementRequests.RecordAsync(group.Third.Client, group.GroupId, group.ThirdRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "SettlementRecorded"));
        }
    }
}
