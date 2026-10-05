using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class JoinWithClaimTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Join_ClaimingAPlainName_Answers200AndCreatesNoRowOfTheirOwn()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            int rowsBefore = await GroupRows.CountMembersAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken, plainId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(setup.GroupId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("groupId").GetGuid());
            Assert.Equal(rowsBefore, await GroupRows.CountMembersAsync(_app, setup.GroupId));
            Assert.Equal(1, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, newcomer.UserId));
        }

        [Fact]
        public async Task Join_ClaimingAPlainName_GivesTheNameRowTheUserAndClaimedAtAndKeepsTheName()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken, plainId);

            Assert.Equal(newcomer.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, plainId, "user_id"));
            Assert.Equal("true", await GroupRows.MemberRowValueAsync(_app, plainId, "claimed_at > now() - interval '1 minute'"));
            Assert.Equal("Marko", await GroupRows.MemberRowValueAsync(_app, plainId, "name"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "removed_at"));
        }

        [Fact]
        public async Task Join_ClaimingAPlainName_WritesMemberJoinedAndMemberClaimedEventsAboutTheNameRowAndOneUsageEvent()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken, plainId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberJoined"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberClaimed"));
            Assert.Equal(plainId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberJoined", "member_id"));
            Assert.Equal(plainId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberClaimed", "member_id"));
            Assert.Equal(newcomer.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberJoined", "actor_user_id"));
            Assert.Equal(newcomer.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberClaimed", "actor_user_id"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, newcomer.UserId, "JoinedViaInvite"));
        }

        [Fact]
        public async Task Join_ClaimingAPlainName_ShowsTheNewcomerOnTheNameRowAndKeepsTheMemberCount()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken, plainId);

            JsonElement body = await GroupRequests.ReadMembersAsync(newcomer.Client, setup.GroupId);
            JsonElement row = GroupRequests.RowWithId(body.GetProperty("members"), plainId);
            Assert.Equal("Marko", row.GetProperty("claimedName").GetString());
            Assert.Equal("Cvetan", row.GetProperty("displayName").GetString());
            Assert.True(row.GetProperty("isYou").GetBoolean());
            Assert.False(body.GetProperty("canClaimNames").GetBoolean());
            Assert.Equal(3, await GroupRequests.MemberCountOfAsync(newcomer.Client, setup.GroupId));
        }

        [Fact]
        public async Task Join_ClaimingANameAnotherPersonAlreadyClaimed_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser holder = await _app.SignUpAsync("Dime");
            Guid claimedId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, holder.UserId, "Marko", holder.UserId, claimedAt: DateTimeOffset.UtcNow);

            await AssertClaimRefusedAsync(setup, claimedId);
        }

        [Fact]
        public async Task Join_ClaimingARemovedPlainName_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid removedId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");

            await AssertClaimRefusedAsync(setup, removedId);
        }

        [Fact]
        public async Task Join_ClaimingAnUnknownMemberId_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await AssertClaimRefusedAsync(setup, Guid.CreateVersion7());
        }

        [Fact]
        public async Task Join_ClaimingAnAccountRow_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await AssertClaimRefusedAsync(setup, setup.MemberRowId);
        }

        [Fact]
        public async Task Join_ClaimingAPlainNameOfAnotherGroup_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(setup.Owner);
            Guid otherPlainId = await GroupRows.InsertMemberAsync(_app, otherGroupId, null, "Marko", setup.Owner.UserId);

            await AssertClaimRefusedAsync(setup, otherPlainId);
        }

        [Fact]
        public async Task Join_CurrentMemberWithAClaimMemberId_Answers200AndIgnoresTheClaim()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(setup.Member.Client, setup.InviteToken, plainId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(setup.GroupId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("groupId").GetGuid());
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Join_CurrentMemberWithAnUnknownClaimMemberId_Answers200AndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(setup.Member.Client, setup.InviteToken, Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Join_PersonWhoLeftWithAClaimMemberId_Answers400CannotClaimAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Left");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(setup.Member.Client, setup.InviteToken, plainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, setup.Member.UserId, "JoinedViaInvite"));
        }

        private async Task AssertClaimRefusedAsync(GroupSetup setup, Guid claimMemberId)
        {
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken, claimMemberId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            Assert.Equal(0, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, newcomer.UserId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, newcomer.UserId, "JoinedViaInvite"));
        }
    }
}
