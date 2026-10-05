using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class LeaveGroupTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Leave_MemberWhoIsNotTheOwner_Answers204AndSetsTheLeftFields()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            int rowsBefore = await GroupRows.CountMembersAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(setup.Member.Client, setup.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("true", await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_at > now() - interval '1 minute'"));
            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_by_user_id"));
            Assert.Equal("Left", await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "end_kind"));
            Assert.Equal(rowsBefore, await GroupRows.CountMembersAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Leave_Member_WritesOneMemberLeftEventByTheMemberAboutTheirRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await GroupRequests.LeaveAsync(setup.Member.Client, setup.GroupId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberLeft"));
            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberLeft", "actor_user_id"));
            Assert.Equal(setup.MemberRowId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberLeft", "member_id"));
        }

        [Fact]
        public async Task Leave_Member_RemovesTheGroupFromTheirListAndTheyNoLongerSeeIt()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await GroupRequests.LeaveAsync(setup.Member.Client, setup.GroupId);

            JsonElement list = await AuthRequests.ReadJsonAsync(await GroupRequests.ListAsync(setup.Member.Client));
            Assert.DoesNotContain(setup.GroupId, GroupRequests.IdsOf(list.GetProperty("groups")));
            HttpResponseMessage response = await GroupRequests.GetAsync(setup.Member.Client, setup.GroupId);
            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Leave_Member_LowersTheMemberCountAndDoesNotListThemAsRemoved()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            int countBefore = await GroupRequests.MemberCountOfAsync(setup.Owner.Client, setup.GroupId);

            await GroupRequests.LeaveAsync(setup.Member.Client, setup.GroupId);

            Assert.Equal(countBefore - 1, await GroupRequests.MemberCountOfAsync(setup.Owner.Client, setup.GroupId));
            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId);
            Assert.DoesNotContain(setup.MemberRowId, GroupRequests.IdsOf(body.GetProperty("members")));
            Assert.Empty(body.GetProperty("removed").EnumerateArray());
        }

        [Fact]
        public async Task Leave_Owner_Answers400OwnerCannotLeaveAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(setup.Owner.Client, setup.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_OWNER_CANNOT_LEAVE);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Leave_PersonWhoAlreadyLeft_Answers404GroupNotFoundAndWritesOneEvent()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.LeaveAsync(setup.Member.Client, setup.GroupId);

            HttpResponseMessage second = await GroupRequests.LeaveAsync(setup.Member.Client, setup.GroupId);

            await ProblemResponse.AssertAsync(second, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberLeft"));
        }

        [Fact]
        public async Task Leave_PersonWhoWasRemoved_Answers404GroupNotFoundAndKeepsTheRemovedRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");

            HttpResponseMessage response = await GroupRequests.LeaveAsync(setup.Member.Client, setup.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal("Removed", await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "end_kind"));
        }

        [Fact]
        public async Task Leave_ThenJoinWithTheInviteLink_ComesBackOnTheSameRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.LeaveAsync(setup.Member.Client, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(setup.Member.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(setup.GroupId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("groupId").GetGuid());
            Assert.Equal(setup.MemberRowId, await GroupRows.CurrentMemberIdAsync(_app, setup.GroupId, setup.Member.UserId));
            Assert.Equal(1, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, setup.Member.UserId));
        }
    }
}
