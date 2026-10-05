using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class MakeOwnerTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task MakeOwner_AccountMember_Answers204AndChangesTheGroupsOwner()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.GroupValueAsync(_app, setup.GroupId, "owner_user_id"));
        }

        [Fact]
        public async Task MakeOwner_AccountMember_WritesOneOwnershipTransferredEventAboutTheNewOwnersRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "OwnershipTransferred"));
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "OwnershipTransferred", "actor_user_id"));
            Assert.Equal(setup.MemberRowId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "OwnershipTransferred", "member_id"));
        }

        [Fact]
        public async Task MakeOwner_AccountMember_SwapsIsOwnerInGetGroupAndKeepsTheOldOwnerAsAMember()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            JsonElement newOwnerView = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(setup.Member.Client, setup.GroupId));
            Assert.True(newOwnerView.GetProperty("isOwner").GetBoolean());
            Assert.Equal(setup.Member.UserId, newOwnerView.GetProperty("ownerUserId").GetGuid());
            JsonElement oldOwnerView = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(setup.Owner.Client, setup.GroupId));
            Assert.False(oldOwnerView.GetProperty("isOwner").GetBoolean());
            Assert.Equal(2, oldOwnerView.GetProperty("memberCount").GetInt32());
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.OwnerRowId, "removed_at"));
        }

        [Fact]
        public async Task MakeOwner_FormerOwner_IsRefusedOwnerActionsWith403()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(setup.Owner.Client, setup.GroupId, "Renamed");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task MakeOwner_NewOwner_IsAllowedOwnerActions()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(setup.Member.Client, setup.GroupId, "Renamed");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task MakeOwner_ThenTheFormerOwnerLeaves_Answers204()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(setup.Owner.Client, setup.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Left", await GroupRows.MemberRowValueAsync(_app, setup.OwnerRowId, "end_kind"));
        }

        [Fact]
        public async Task MakeOwner_ThenTheNewOwnerTriesToLeave_Answers400OwnerCannotLeave()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            HttpResponseMessage response = await GroupRequests.LeaveAsync(setup.Member.Client, setup.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_OWNER_CANNOT_LEAVE);
        }

        [Fact]
        public async Task MakeOwner_PlainName_Answers400NotAccountAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, plainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_ACCOUNT);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task MakeOwner_TheCurrentOwner_Answers400AlreadyOwnerAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, setup.OwnerRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_ALREADY_OWNER);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        [InlineData("SetAside")]
        public async Task MakeOwner_RowThatIsNoLongerCurrent_Answers404MemberNotFoundAndWritesNothing(string endKind)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, endKind);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task MakeOwner_UnknownMemberId_Answers404MemberNotFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task MakeOwner_AccountMemberOfAnotherGroup_Answers404MemberNotFoundAndChangesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser otherOwner = await _app.SignUpAsync("Cvetan");
            Guid otherGroupId = await _app.CreateGroupAsync(otherOwner);
            Guid otherRowId = await GroupRows.CurrentMemberIdAsync(_app, otherGroupId, otherOwner.UserId);

            HttpResponseMessage response = await GroupRequests.MakeOwnerAsync(setup.Owner.Client, setup.GroupId, otherRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.GroupValueAsync(_app, setup.GroupId, "owner_user_id"));
        }

        [Fact]
        public async Task MakeOwner_MemberWhoIsNotTheOwner_Answers403GroupNotOwnerAndChangesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.MakeOwnerAsync(setup.Member.Client, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }
    }
}
