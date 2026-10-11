using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RestoreSettlementTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Restore_ByWhoeverRecordedIt_Answers204WithNoBody()
        {
            (SettlementGroup group, Guid settlementId) = await RecordByBojanAndDeleteByBojanAsync();

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Member.Client, group.GroupId, settlementId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Restore_ByTheOwnerOfASettlementRecordedAndDeletedByAnotherMember_Answers204()
        {
            (SettlementGroup group, Guid settlementId) = await RecordByBojanAndDeleteByBojanAsync();

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Restore_ByWhoeverRecordedItAfterTheOwnerDeletedIt_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Member.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Restore_ByAnUnrelatedMember_Answers403SettlementNotAllowedAndChangesNothing()
        {
            (SettlementGroup group, Guid settlementId) = await RecordByBojanAndDeleteByBojanAsync();
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Third.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Restore_ClearsDeletedAtAndDeletedByAndKeepsTheStatusConfirmed()
        {
            (SettlementGroup group, Guid settlementId) = await RecordByBojanAndDeleteByBojanAsync();

            await SettlementSteps.RestoreAsync(group.Owner, group.GroupId, settlementId);

            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            Assert.Null(await SettlementRows.ValueAsync(_app, settlementId, "deleted_by_user_id"));
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Restore_PutsTheSettlementBackIntoTheBalancesAndTheyStillSumToZero()
        {
            (SettlementGroup group, Guid settlementId) = await RecordByBojanAndDeleteByBojanAsync();
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);

            await SettlementSteps.RestoreAsync(group.Member, group.GroupId, settlementId);

            Assert.Equal([30_000L, -30_000L, 0L, 0L, 0L], await SettlementSteps.BalanceMinorsAsync(group.Owner, group.GroupId, "MKD"));
        }

        [Fact]
        public async Task Restore_BringsTheSettlementBackIntoTheList()
        {
            (SettlementGroup group, Guid settlementId) = await RecordByBojanAndDeleteByBojanAsync();
            Assert.Empty(SettlementJson.IdsOf(await SettlementSteps.ReadListAsync(group.Owner, group.GroupId)));

            await SettlementSteps.RestoreAsync(group.Member, group.GroupId, settlementId);

            Assert.Equal([settlementId], SettlementJson.IdsOf(await SettlementSteps.ReadListAsync(group.Owner, group.GroupId)));
        }

        [Fact]
        public async Task Restore_WritesNoSecondConfirmedUsageEvent()
        {
            (SettlementGroup group, Guid settlementId) = await RecordByBojanAndDeleteByBojanAsync();

            await SettlementSteps.RestoreAsync(group.Member, group.GroupId, settlementId);

            Assert.Equal(1, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Restore_ThirtyDaysAfterTheDelete_StillAnswers204BecauseSettlementsHaveNoRestoreLimit()
        {
            (SettlementGroup group, Guid settlementId) = await RecordByBojanAndDeleteByBojanAsync();
            _app.Clock.Advance(TimeSpan.FromDays(30));

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Member.Client, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Restore_ThenDeleteAgain_Works()
        {
            (SettlementGroup group, Guid settlementId) = await RecordByBojanAndDeleteByBojanAsync();
            await SettlementSteps.RestoreAsync(group.Member, group.GroupId, settlementId);

            await SettlementSteps.DeleteAsync(group.Member, group.GroupId, settlementId);

            Assert.NotNull(await SettlementRows.ValueAsync(_app, settlementId, "deleted_at"));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Restore_SettlementThatIsNotDeleted_Answers400NotDeletedAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_DELETED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Restore_PendingSettlement_Answers400NotDeleted()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_DELETED);
        }

        [Fact]
        public async Task Restore_Twice_SecondAnswers400NotDeletedAndWritesOneRestoredEvent()
        {
            (SettlementGroup group, Guid settlementId) = await RecordByBojanAndDeleteByBojanAsync();
            await SettlementSteps.RestoreAsync(group.Member, group.GroupId, settlementId);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Member.Client, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.SETTLEMENT_NOT_DELETED);
            Assert.Equal(1, await SettlementRows.CountEventsAsync(_app, settlementId, "SettlementRestored"));
        }

        [Fact]
        public async Task Restore_SettlementThatDoesNotExist_Answers404SettlementNotFound()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
        }

        [Fact]
        public async Task Restore_DeletedSettlementOfAnotherGroup_Answers404SettlementNotFoundAndKeepsItDeleted()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            (SettlementGroup other, Guid settlementInOther) = await RecordByBojanAndDeleteByBojanAsync();

            HttpResponseMessage response = await SettlementRequests.RestoreAsync(group.Owner.Client, group.GroupId, settlementInOther);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
            Assert.NotNull(await SettlementRows.ValueAsync(_app, settlementInOther, "deleted_at"));
            Assert.Equal(other.GroupId.ToString(), await SettlementRows.ValueAsync(_app, settlementInOther, "group_id"));
        }

        [Fact]
        public async Task Restore_ReturnsTheSettlementInTheListWithTheSameFieldsItHadBeforeTheDelete()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId);
            JsonElement before = SettlementJson.RowWithId(await SettlementSteps.ReadListAsync(group.Owner, group.GroupId), settlementId);
            string beforeText = before.GetRawText();
            await SettlementSteps.DeleteAsync(group.Member, group.GroupId, settlementId);

            await SettlementSteps.RestoreAsync(group.Member, group.GroupId, settlementId);

            JsonElement after = SettlementJson.RowWithId(await SettlementSteps.ReadListAsync(group.Owner, group.GroupId), settlementId);
            Assert.Equal(beforeText, after.GetRawText());
        }

        private async Task<(SettlementGroup Group, Guid SettlementId)> RecordByBojanAndDeleteByBojanAsync()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId);
            await SettlementSteps.DeleteAsync(group.Member, group.GroupId, settlementId);

            return (group, settlementId);
        }
    }
}
