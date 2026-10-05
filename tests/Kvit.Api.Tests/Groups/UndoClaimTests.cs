using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class UndoClaimTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task UndoClaim_ClaimWithASetAsideRow_Answers204AndTheNameRowIsPlainAgain()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            HttpResponseMessage response = await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, plainId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "claimed_at"));
            Assert.Equal("Marko", await GroupRows.MemberRowValueAsync(_app, plainId, "name"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "removed_at"));
        }

        [Fact]
        public async Task UndoClaim_ClaimWithASetAsideRow_RestoresTheSameOwnRowOfThePerson()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);
            int rowsBefore = await GroupRows.CountMembersAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, plainId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(setup.MemberRowId, await GroupRows.CurrentMemberIdAsync(_app, setup.GroupId, setup.Member.UserId));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_by_user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "end_kind"));
            Assert.Equal(rowsBefore, await GroupRows.CountMembersAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task UndoClaim_ClaimWithASetAsideRow_KeepsThePersonInTheGroupAndShowsTheNameAsPlain()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, plainId);

            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(setup.Member.Client, setup.GroupId)).StatusCode);
            Assert.Equal(3, await GroupRequests.MemberCountOfAsync(setup.Owner.Client, setup.GroupId));
            JsonElement members = (await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId)).GetProperty("members");
            Assert.True(GroupRequests.RowWithId(members, plainId).GetProperty("isNameOnly").GetBoolean());
            Assert.Equal(setup.Member.UserId, GroupRequests.RowWithId(members, setup.MemberRowId).GetProperty("userId").GetGuid());
        }

        [Fact]
        public async Task UndoClaim_Owner_WritesOneClaimUndoneEventByTheOwnerAboutTheNameRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, plainId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "ClaimUndone"));
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "ClaimUndone", "actor_user_id"));
            Assert.Equal(plainId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "ClaimUndone", "member_id"));
        }

        [Fact]
        public async Task UndoClaim_ClaimMadeAtJoinWithNoSetAsideRow_CreatesANewOwnRowWithTheAccountName()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            Guid claimedId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, newcomer.UserId, "Marko", newcomer.UserId, claimedAt: DateTimeOffset.UtcNow);
            int rowsBefore = await GroupRows.CountMembersAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, claimedId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, claimedId, "user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, claimedId, "claimed_at"));
            Assert.Equal("Marko", await GroupRows.MemberRowValueAsync(_app, claimedId, "name"));
            Assert.Equal(rowsBefore + 1, await GroupRows.CountMembersAsync(_app, setup.GroupId));
            Guid ownRowId = await GroupRows.CurrentMemberIdAsync(_app, setup.GroupId, newcomer.UserId);
            Assert.NotEqual(claimedId, ownRowId);
            Assert.Equal("Cvetan", await GroupRows.MemberRowValueAsync(_app, ownRowId, "name"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, ownRowId, "claimed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, ownRowId, "removed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, ownRowId, "end_kind"));
            Assert.Equal("true", await GroupRows.MemberRowValueAsync(_app, ownRowId, "joined_at > now() - interval '1 minute'"));
        }

        [Fact]
        public async Task UndoClaim_ClaimMadeAtJoin_KeepsThePersonInTheGroup()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            Guid claimedId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, newcomer.UserId, "Marko", newcomer.UserId, claimedAt: DateTimeOffset.UtcNow);

            await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, claimedId);

            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(newcomer.Client, setup.GroupId)).StatusCode);
            Assert.Equal(4, await GroupRequests.MemberCountOfAsync(setup.Owner.Client, setup.GroupId));
            JsonElement members = (await GroupRequests.ReadMembersAsync(newcomer.Client, setup.GroupId)).GetProperty("members");
            Assert.True(GroupRequests.RowWithId(members, claimedId).GetProperty("isNameOnly").GetBoolean());
        }

        [Fact]
        public async Task UndoClaim_SeveralSetAsideRowsOfThePerson_RestoresTheLatestOne()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "SetAside");
            Guid olderId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, setup.Member.UserId, "Older", setup.Member.UserId, DateTimeOffset.UtcNow.AddDays(-3), "SetAside");
            Guid claimedId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, setup.Member.UserId, "Marko", setup.Member.UserId, claimedAt: DateTimeOffset.UtcNow);

            await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, claimedId);

            Assert.Equal(setup.MemberRowId, await GroupRows.CurrentMemberIdAsync(_app, setup.GroupId, setup.Member.UserId));
            Assert.Equal("SetAside", await GroupRows.MemberRowValueAsync(_app, olderId, "end_kind"));
        }

        [Fact]
        public async Task UndoClaim_ThenClaimAgain_Answers204()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);
            await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, plainId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, plainId, "user_id"));
        }

        [Fact]
        public async Task UndoClaim_PlainNameThatWasNeverClaimed_Answers400NotClaimedAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, plainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_CLAIMED);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task UndoClaim_OrdinaryAccountRow_Answers400NotClaimedAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_CLAIMED);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task UndoClaim_UnknownMemberId_Answers404MemberNotFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        [InlineData("SetAside")]
        public async Task UndoClaim_ClaimedRowThatIsNoLongerCurrent_Answers404MemberNotFoundAndWritesNothing(string endKind)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid claimedId = await GroupRows.InsertMemberAsync(
                _app, setup.GroupId, setup.Member.UserId, "Marko", setup.Member.UserId, DateTimeOffset.UtcNow, endKind, claimedAt: DateTimeOffset.UtcNow.AddHours(-1));
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, claimedId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task UndoClaim_SetAsideOwnRowThatWasNeverClaimed_Answers404MemberNotFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "SetAside");

            HttpResponseMessage response = await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task UndoClaim_ClaimedRowOfAnotherGroup_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(setup.Owner);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            Guid otherClaimedId = await GroupRows.InsertMemberAsync(_app, otherGroupId, newcomer.UserId, "Marko", newcomer.UserId, claimedAt: DateTimeOffset.UtcNow);
            string before = await GroupRows.SnapshotAsync(_app, otherGroupId);

            HttpResponseMessage response = await GroupRequests.UndoClaimAsync(setup.Owner.Client, setup.GroupId, otherClaimedId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, otherGroupId));
        }

        [Fact]
        public async Task UndoClaim_ByTheClaimerWhoIsNotTheOwner_Answers403GroupNotOwnerAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid claimedId = await _app.SetUpClaimedNameAsync(setup, "Marko");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.UndoClaimAsync(setup.Member.Client, setup.GroupId, claimedId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }
    }
}
