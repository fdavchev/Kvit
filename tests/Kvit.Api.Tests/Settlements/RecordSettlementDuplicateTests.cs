using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RecordSettlementDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Record_SameClientRequestIdAndBodyTwice_Answers200BothTimesWithTheSameSettlement()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();

            HttpResponseMessage first = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            HttpResponseMessage second = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            JsonElement firstBody = await AuthRequests.ReadJsonAsync(first);
            JsonElement secondBody = await AuthRequests.ReadJsonAsync(second);
            Assert.Equal(firstBody.GetRawText(), secondBody.GetRawText());
        }

        [Fact]
        public async Task Record_SameClientRequestIdTwiceAsReceiver_WritesOneRowOneRecordedEventAndOneUsageEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, clientRequestId: clientRequestId);

            await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementRecorded"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "SettlementRecorded"));
            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
            Assert.Equal([-30_000L, 30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Record_SameClientRequestIdTwiceAsPayer_WritesOnePendingRowAndOneRecordedEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, clientRequestId: clientRequestId);

            await SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal("Pending", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "SettlementRecorded"));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Record_SameClientRequestIdWithOtherFields_AnswersTheFirstSettlementUnchanged()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            HttpResponseMessage firstResponse = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            Guid settlementId = (await AuthRequests.ReadJsonAsync(firstResponse)).GetProperty("id").GetGuid();

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MarkoRowId, group.OwnerRowId, 500_000, ExpenseInputs.Eur, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(settlementId, body.GetProperty("id").GetGuid());
            Assert.Equal(group.MemberRowId, body.GetProperty("fromMemberId").GetGuid());
            Assert.Equal(30_000, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal("30000", await SettlementRows.ValueAsync(_app, settlementId, "amount_minor"));
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_ClientRequestIdAlreadyUsedByAnotherPersonInTheSameGroup_Answers400AndWritesNothingAndRevealsNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, clientRequestId: clientRequestId);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Third.Client, group.GroupId, group.ThirdRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.DoesNotContain(settlementId.ToString(), await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "recorded_by_user_id"));
        }

        [Fact]
        public async Task Record_ClientRequestIdAlreadyUsedInAnotherGroup_Answers400AndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SettlementGroup other = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, clientRequestId: clientRequestId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(other.Owner.Client, other.GroupId, other.MemberRowId, other.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, await SettlementRows.CountAsync(_app, other.GroupId));
        }

        [Fact]
        public async Task Record_ClientRequestIdWhoseFirstTryWasRejected_IsFreeForTheCorrectedRequest()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            HttpResponseMessage rejected = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, "USD", clientRequestId);
            await ProblemResponse.AssertAsync(rejected, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_CURRENCY_INVALID);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_ClientRequestIdWhoseFirstTryWasForbidden_IsFreeForTheCorrectedRequest()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            HttpResponseMessage forbidden = await SettlementRequests.RecordAsync(group.Third.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);
            await ProblemResponse.AssertAsync(forbidden, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_TwoDifferentClientRequestIds_SaveTwoSettlements()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, Guid.CreateVersion7());
            await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, Guid.CreateVersion7());

            Assert.Equal(2, await SettlementRows.CountAsync(_app, group.GroupId));
        }
    }
}
