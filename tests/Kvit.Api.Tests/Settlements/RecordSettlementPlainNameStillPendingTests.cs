using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RecordSettlementPlainNameStillPendingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(ExpenseInputs.Mkd, 30_000)]
        [InlineData(ExpenseInputs.Eur, 1_550)]
        public async Task Record_ByAMemberWhoIsNotTheOwnerAsThePayerToAPlainName_Answers200WithAPendingSettlement(string currency, long amountMinor)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.MemberRowId, group.MarkoRowId, amountMinor, currency);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Pending", body.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("resolvedAt").ValueKind);
            Assert.Equal(group.Member.UserId, body.GetProperty("recordedByUserId").GetGuid());
            Assert.Equal(currency, body.GetProperty("currency").GetString());
            Assert.Equal(amountMinor, body.GetProperty("amountMinor").GetInt64());
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Confirm_ByTheOwnerForAPlainNameReceiverOfAMembersPendingPayment_Answers204AndMovesTheBalances()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "resolved_by_user_id"));
            Assert.Equal([0L, 30_000L, -30_000L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Reject_ByTheOwnerForAPlainNameReceiverOfAMembersPendingPayment_Answers204AndLeavesTheBalances()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.RejectAsync(group.Owner.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Rejected", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Cancel_ByThePayerOfAPendingPaymentToAPlainName_Answers204AndLeavesTheBalances()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

            HttpResponseMessage response = await SettlementRequests.CancelAsync(group.Member.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Cancelled", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Theory]
        [InlineData(ExpenseInputs.Mkd, 30_000)]
        [InlineData(ExpenseInputs.Eur, 1_550)]
        public async Task Record_ByTheOwnerAsThePayerToAnAccountMember_Answers200WithAPendingSettlement(string currency, long amountMinor)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.OwnerRowId, group.MemberRowId, amountMinor, currency);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Pending", body.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("resolvedAt").ValueKind);
            Assert.Equal(group.Owner.UserId, body.GetProperty("recordedByUserId").GetGuid());
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Confirm_ByTheAccountMemberWhoReceivedTheOwnersPendingPayment_Answers204AndMovesTheBalances()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingAnaToBojanAsync(group);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Member.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal(group.Member.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "resolved_by_user_id"));
            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Confirm_ByTheOwnerOfHerOwnPaymentToAnAccountMember_Answers403SettlementNotAllowed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingAnaToBojanAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.ConfirmAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }
    }
}
