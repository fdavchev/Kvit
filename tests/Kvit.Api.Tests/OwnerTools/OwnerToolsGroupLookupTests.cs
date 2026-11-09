using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsGroupLookupTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        [Fact]
        public async Task Group_Open_AnswersExactlyTheNineDocumentedFields()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement group = await ReadAsync(setup.GroupId);

            Assert.Equal(OwnerToolsRequests.GroupFieldNames, GroupRequests.PropertyNamesOf(group));
        }

        [Fact]
        public async Task Group_Open_AnswersItsNameEmojiStatusOwnerAndMemberCount()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement group = await ReadAsync(setup.GroupId);

            Assert.Equal(setup.GroupId, group.GetProperty("id").GetGuid());
            Assert.Equal(GroupRows.DefaultName, group.GetProperty("name").GetString());
            Assert.Equal(GroupRows.DefaultEmoji, group.GetProperty("emoji").GetString());
            Assert.Equal("Open", group.GetProperty("status").GetString());
            Assert.Equal(GroupsApp.OwnerName, group.GetProperty("ownerName").GetString());
            Assert.Equal(2, group.GetProperty("memberCount").GetInt32());
            Assert.False(group.GetProperty("isDeleted").GetBoolean());
            Assert.Equal(JsonValueKind.Null, group.GetProperty("closedByAdminAt").ValueKind);
        }

        [Fact]
        public async Task Group_Open_AnswersTheCreationTimeStoredInTheDatabase()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            DateTimeOffset? stored = await DeletionRows.TimeOfAsync(_app, "groups WHERE id = @id", "created_at", new NpgsqlParameter("id", setup.GroupId));

            JsonElement group = await ReadAsync(setup.GroupId);

            Assert.Equal(stored, DateTimeOffset.Parse(group.GetProperty("createdAt").GetString()!, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        public async Task Group_MemberCount_LeavesOutAPersonWhoseMembershipEnded(string endKind)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            JsonElement answer = await ReadAsync(group.GroupId);

            Assert.Equal(1, answer.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Group_MemberCount_CountsPlainNamesToo()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko", "Cveta");

            JsonElement answer = await ReadAsync(group.GroupId);

            Assert.Equal(4, answer.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Group_OwnerNameAfterOwnershipMoved_IsTheNewOwnersName()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.AssertNoContentAsync(await GroupRequests.MakeOwnerAsync(group.Owner.Client, group.GroupId, group.MemberRowId));

            JsonElement answer = await ReadAsync(group.GroupId);

            Assert.Equal(GroupsApp.MemberName, answer.GetProperty("ownerName").GetString());
        }

        [Fact]
        public async Task Group_Closing_AnswersStatusClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            JsonElement answer = await ReadAsync(group.GroupId);

            Assert.Equal("Closing", answer.GetProperty("status").GetString());
        }

        [Fact]
        public async Task Group_Finished_AnswersStatusFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            JsonElement answer = await ReadAsync(group.GroupId);

            Assert.Equal("Finished", answer.GetProperty("status").GetString());
        }

        [Fact]
        public async Task Group_ClosingWhoseTimeRanOut_AnswersFinishedAndTheLookupWritesNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(ClosingSteps.ClosingWindow + ClosingSteps.OneMinute);
            string before = await GroupRows.SnapshotAsync(_app, group.GroupId);

            JsonElement answer = await ReadAsync(group.GroupId);

            Assert.Equal("Finished", answer.GetProperty("status").GetString());
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal("Closing", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
        }

        [Fact]
        public async Task Group_DeletedByItsOwner_IsFoundWithIsDeletedTrue()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await SettlementSteps.AssertNoContentAsync(await GroupRequests.DeleteAsync(setup.Owner.Client, setup.GroupId));

            JsonElement answer = await ReadAsync(setup.GroupId);

            Assert.True(answer.GetProperty("isDeleted").GetBoolean());
            Assert.Equal(GroupRows.DefaultName, answer.GetProperty("name").GetString());
        }

        [Fact]
        public async Task Group_DeletedLongAgoAndPastTheRestoreWindow_IsStillFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.SetDeletedAsync(_app, setup.GroupId, setup.Owner.UserId, 90);

            JsonElement answer = await ReadAsync(setup.GroupId);

            Assert.True(answer.GetProperty("isDeleted").GetBoolean());
        }

        [Fact]
        public async Task Group_NotDeleted_AnswersIsDeletedFalse()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement answer = await ReadAsync(setup.GroupId);

            Assert.False(answer.GetProperty("isDeleted").GetBoolean());
        }

        [Fact]
        public async Task Group_Unknown_Answers404GroupNotFound()
        {
            SignedInUser admin = await _app.SharedAdminAsync();

            HttpResponseMessage response = await OwnerToolsRequests.GroupAsync(admin.Client, Guid.NewGuid());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, OwnerToolsRequests.GroupNotFoundCode);
        }

        [Fact]
        public async Task Group_AdminIsNotAMember_StillGetsTheAnswerAndIsNotAddedToTheGroup()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser admin = await _app.SharedAdminAsync();

            await ReadAsync(setup.GroupId);

            Assert.Equal(0, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, admin.UserId));
        }

        [Fact]
        public async Task Group_ClosedByTheOwnerOfKvit_AnswersTheCloseTimeAndNullAgainAfterReopening()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);
            DateTimeOffset closedAt = _app.Clock.GetUtcNow();

            JsonElement whileClosed = await ReadAsync(setup.GroupId);
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.ReopenGroupAsync(admin, setup.GroupId);
            JsonElement afterReopening = await ReadAsync(setup.GroupId);

            Assert.Equal(closedAt, DateTimeOffset.Parse(whileClosed.GetProperty("closedByAdminAt").GetString()!, CultureInfo.InvariantCulture));
            Assert.Equal(JsonValueKind.Null, afterReopening.GetProperty("closedByAdminAt").ValueKind);
        }

        [Fact]
        public async Task Group_Lookup_WritesNoLogRowAndChangesNoGroupRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);
            int logRowsBefore = await OwnerToolsRows.CountActionsAsync(_app);

            await ReadAsync(setup.GroupId);
            await ReadAsync(setup.GroupId);

            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            Assert.Equal(logRowsBefore, await OwnerToolsRows.CountActionsAsync(_app));
        }

        private async Task<JsonElement> ReadAsync(Guid groupId)
        {
            SignedInUser admin = await _app.SharedAdminAsync();

            return await OwnerToolsRequests.ReadGroupAsync(admin.Client, groupId);
        }
    }
}
