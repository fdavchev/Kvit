using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Guards;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RecordSettlementLateRepeatTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Record_RepeatAfterTheGroupBecameClosing_Answers200WithTheSameSettlementAndLeavesTheClosingAlone()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId, clientRequestId: clientRequestId);
            await GuardStates.ForceClosingAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.OwnerRowId, group.MemberRowId, SettlementRequests.DefaultAmountMinor, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(settlementId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid());
            Assert.Equal("Closing", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_RepeatAfterTheGroupFinished_Answers200WithTheSameSettlementAndLeavesTheGroupFinished()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId, clientRequestId: clientRequestId);
            await GuardStates.ForceFinishedAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.OwnerRowId, group.MemberRowId, SettlementRequests.DefaultAmountMinor, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(settlementId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid());
            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_RepeatAfterTheGroupWasDeleted_Answers404GroupNotFoundEvenForTheOwner()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, clientRequestId: clientRequestId);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, SettlementRequests.DefaultAmountMinor, ExpenseInputs.Mkd, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        public async Task Record_RepeatAfterTheCallerNoLongerBelongsToTheGroup_Answers404GroupNotFound(string endKind)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId, clientRequestId: clientRequestId);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.OwnerRowId, group.MemberRowId, SettlementRequests.DefaultAmountMinor, ExpenseInputs.Mkd, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_RepeatAfterTheSettlementWasDeleted_Answers404SettlementNotFoundAndSavesNothingNew()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId, clientRequestId: clientRequestId);
            await SettlementSteps.DeleteAsync(group.Member, group.GroupId, settlementId);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.OwnerRowId, group.MemberRowId, SettlementRequests.DefaultAmountMinor, ExpenseInputs.Mkd, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_RepeatAfterAPendingPaymentWasConfirmed_Answers200WithTheCurrentStatusConfirmed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, clientRequestId: clientRequestId);
            Assert.Equal("Pending", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, SettlementRequests.DefaultAmountMinor, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(settlementId, body.GetProperty("id").GetGuid());
            Assert.Equal("Confirmed", body.GetProperty("status").GetString());
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_RepeatAfterAPendingPaymentWasRejected_Answers200WithTheCurrentStatusRejected()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, clientRequestId: clientRequestId);
            Assert.Equal("Pending", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            await SettlementSteps.RejectAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Member.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, SettlementRequests.DefaultAmountMinor, ExpenseInputs.Mkd, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(settlementId, body.GetProperty("id").GetGuid());
            Assert.Equal("Rejected", body.GetProperty("status").GetString());
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
        }
    }
}
