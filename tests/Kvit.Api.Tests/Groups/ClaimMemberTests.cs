using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class ClaimMemberTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Claim_PlainName_Answers204AndSetsTheCallersOwnRowAside()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("SetAside", await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "end_kind"));
            Assert.Equal("true", await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_at > now() - interval '1 minute'"));
            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_by_user_id"));
        }

        [Fact]
        public async Task Claim_PlainName_GivesTheNameRowTheCallersUserAndClaimedAtAndKeepsTheName()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);

            await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, plainId, "user_id"));
            Assert.Equal("true", await GroupRows.MemberRowValueAsync(_app, plainId, "claimed_at > now() - interval '1 minute'"));
            Assert.Equal("Marko", await GroupRows.MemberRowValueAsync(_app, plainId, "name"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "removed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "end_kind"));
        }

        [Fact]
        public async Task Claim_PlainName_WritesOneMemberClaimedEventByTheCallerAboutTheNameRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);

            await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberClaimed"));
            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberClaimed", "actor_user_id"));
            Assert.Equal(plainId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberClaimed", "member_id"));
        }

        [Fact]
        public async Task Claim_PlainName_LowersTheMemberCountByOneBecauseTwoRowsBecomeOnePerson()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            int countBefore = await GroupRequests.MemberCountOfAsync(setup.Member.Client, setup.GroupId);

            await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            Assert.Equal(3, countBefore);
            Assert.Equal(2, await GroupRequests.MemberCountOfAsync(setup.Member.Client, setup.GroupId));
        }

        [Fact]
        public async Task Claim_PlainName_ShowsTheNameInTheMembersListAsTheCallersWithClaimedName()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);

            await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Member.Client, setup.GroupId);
            JsonElement members = body.GetProperty("members");
            JsonElement row = GroupRequests.RowWithId(members, plainId);
            Assert.Equal("Marko", row.GetProperty("claimedName").GetString());
            Assert.Equal(setup.Member.UserId, row.GetProperty("userId").GetGuid());
            Assert.True(row.GetProperty("isYou").GetBoolean());
            Assert.False(row.GetProperty("isNameOnly").GetBoolean());
            Assert.Equal(GroupsApp.MemberName, row.GetProperty("displayName").GetString());
            Assert.DoesNotContain(setup.MemberRowId, GroupRequests.IdsOf(members));
            Assert.False(body.GetProperty("canClaimNames").GetBoolean());
        }

        [Fact]
        public async Task Claim_PlainName_KeepsTheCallerInTheGroup()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);

            HttpResponseMessage claim = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            Assert.Equal(HttpStatusCode.NoContent, claim.StatusCode);
            HttpResponseMessage response = await GroupRequests.GetAsync(setup.Member.Client, setup.GroupId);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Claim_TargetIsAnAccountRow_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, setup.OwnerRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Claim_TargetIsTheCallersOwnRow_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Claim_NameAnotherPersonAlreadyClaimed_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser third = await _app.SignUpAsync("Cvetan");
            Guid claimedId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, third.UserId, "Marko", third.UserId, claimedAt: DateTimeOffset.UtcNow);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, claimedId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Claim_RemovedPlainName_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Claim_UnknownMemberId_Answers404MemberNotFoundAndKeepsTheCallersRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "end_kind"));
        }

        [Fact]
        public async Task Claim_PlainNameOfAnotherGroup_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(setup.Owner);
            Guid otherPlainId = await GroupRows.InsertMemberAsync(_app, otherGroupId, null, "Marko", setup.Owner.UserId);
            string before = await GroupRows.SnapshotAsync(_app, otherGroupId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, otherPlainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, otherGroupId));
        }

        [Fact]
        public async Task Claim_CallerWhoAlreadyHoldsAClaimedName_Answers400CannotClaimAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await _app.SetUpClaimedNameAsync(setup, "Marko");
            Guid otherPlainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Zora", setup.Owner.UserId);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, otherPlainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Claim_NonMember_Answers404GroupNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(stranger.Client, setup.GroupId, plainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Claim_PersonWhoWasRemoved_Answers404GroupNotFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");

            HttpResponseMessage response = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }
    }
}
