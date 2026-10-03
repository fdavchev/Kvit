using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class RemoveMemberTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Remove_OwnerRemovesAPlainName_Answers204AndSetsTheRemovedFields()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            int rowsBefore = await GroupRows.CountMembersAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, plainId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("true", await GroupRows.MemberRowValueAsync(_app, plainId, "removed_at > now() - interval '1 minute'"));
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, plainId, "removed_by_user_id"));
            Assert.Equal("Removed", await GroupRows.MemberRowValueAsync(_app, plainId, "end_kind"));
            Assert.Equal(rowsBefore, await GroupRows.CountMembersAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Remove_OwnerRemovesAnAccountMember_SetsTheRemovedFieldsAndKeepsTheUser()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("true", await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_at > now() - interval '1 minute'"));
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_by_user_id"));
            Assert.Equal("Removed", await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "end_kind"));
            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "user_id"));
        }

        [Fact]
        public async Task Remove_Owner_WritesOneMemberRemovedEventByTheOwnerAboutTheRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberRemoved"));
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberRemoved", "actor_user_id"));
            Assert.Equal(setup.MemberRowId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberRemoved", "member_id"));
        }

        [Fact]
        public async Task Remove_Owner_MovesTheMemberFromMembersToRemovedAndLowersTheMemberCount()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            int countBefore = await GroupRequests.MemberCountOfAsync(setup.Owner.Client, setup.GroupId);

            await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId);
            Assert.DoesNotContain(setup.MemberRowId, GroupRequests.IdsOf(body.GetProperty("members")));
            Assert.Equal([setup.MemberRowId], GroupRequests.IdsOf(body.GetProperty("removed")));
            Assert.Equal(countBefore - 1, await GroupRequests.MemberCountOfAsync(setup.Owner.Client, setup.GroupId));
        }

        [Fact]
        public async Task Remove_AccountMember_NoLongerSeesTheGroup()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            HttpResponseMessage getResponse = await GroupRequests.GetAsync(setup.Member.Client, setup.GroupId);
            await ProblemResponse.AssertAsync(getResponse, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            HttpResponseMessage membersResponse = await GroupRequests.ListMembersAsync(setup.Member.Client, setup.GroupId);
            await ProblemResponse.AssertAsync(membersResponse, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            JsonElement list = await AuthRequests.ReadJsonAsync(await GroupRequests.ListAsync(setup.Member.Client));
            Assert.DoesNotContain(setup.GroupId, GroupRequests.IdsOf(list.GetProperty("groups")));
        }

        [Fact]
        public async Task Remove_AccountMember_CannotUseTheInviteLinkAnyMore()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(setup.Member.Client, setup.InviteToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.INVITE_REMOVED);
            Assert.Equal("Removed", await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "end_kind"));
        }

        [Fact]
        public async Task Remove_OwnerRemovesThemself_Answers400MemberIsOwnerAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, setup.OwnerRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_IS_OWNER);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Remove_MemberWhoIsNotTheOwner_Answers403GroupNotOwnerAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(setup.Member.Client, setup.GroupId, plainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Remove_UnknownMemberId_Answers404MemberNotFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Remove_MemberOfAnotherGroup_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(setup.Owner);
            Guid otherPlainId = await GroupRows.InsertMemberAsync(_app, otherGroupId, null, "Marko", setup.Owner.UserId);
            string before = await GroupRows.SnapshotAsync(_app, otherGroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, otherPlainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, otherGroupId));
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        [InlineData("SetAside")]
        public async Task Remove_RowThatIsNoLongerCurrent_Answers404MemberNotFound(string endKind)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, endKind);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Remove_TwiceInARow_Answers404MemberNotFoundTheSecondTimeAndWritesOneEvent()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            HttpResponseMessage second = await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(second, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberRemoved"));
        }
    }
}
