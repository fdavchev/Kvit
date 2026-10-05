using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class JoinGroupTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Join_NewPerson_Answers200WithExactlyTheGroupId()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["groupId"], GroupRequests.PropertyNamesOf(body));
            Assert.Equal(setup.GroupId, body.GetProperty("groupId").GetGuid());
        }

        [Fact]
        public async Task Join_NewPerson_AddsOneRowWithTheAccountNameJoinedAtAndAddedByThemself()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            int rowsBefore = await GroupRows.CountMembersAsync(_app, setup.GroupId);

            await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken);

            Assert.Equal(rowsBefore + 1, await GroupRows.CountMembersAsync(_app, setup.GroupId));
            Guid rowId = await GroupRows.CurrentMemberIdAsync(_app, setup.GroupId, newcomer.UserId);
            Assert.Equal("Cvetan", await GroupRows.MemberRowValueAsync(_app, rowId, "name"));
            Assert.Equal("true", await GroupRows.MemberRowValueAsync(_app, rowId, "joined_at > now() - interval '1 minute'"));
            Assert.Equal(newcomer.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, rowId, "added_by_user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, rowId, "claimed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, rowId, "removed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, rowId, "end_kind"));
        }

        [Fact]
        public async Task Join_NewPerson_WritesOneMemberJoinedEventAboutTheNewRowAndNoClaimEvent()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken);

            Guid rowId = await GroupRows.CurrentMemberIdAsync(_app, setup.GroupId, newcomer.UserId);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberJoined"));
            Assert.Equal(newcomer.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberJoined", "actor_user_id"));
            Assert.Equal(rowId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberJoined", "member_id"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberClaimed"));
        }

        [Fact]
        public async Task Join_NewPerson_WritesOneJoinedViaInviteUsageEvent()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken);

            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, newcomer.UserId, "JoinedViaInvite"));
        }

        [Fact]
        public async Task Join_NewPerson_SeesTheGroupAndTheMemberCountRises()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken);

            Assert.Equal(3, await GroupRequests.MemberCountOfAsync(newcomer.Client, setup.GroupId));
            JsonElement list = await AuthRequests.ReadJsonAsync(await GroupRequests.ListAsync(newcomer.Client));
            Assert.Contains(setup.GroupId, GroupRequests.IdsOf(list.GetProperty("groups")));
        }

        [Fact]
        public async Task Join_BodyWithoutTheClaimMemberIdProperty_Answers200()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(newcomer.Client, GroupRequests.JoinPath, $"{{\"token\":\"{setup.InviteToken}\"}}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Join_Twice_Answers200WithTheSameGroupIdAndChangesNothingTheSecondTime()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage second = await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal(setup.GroupId, (await AuthRequests.ReadJsonAsync(second)).GetProperty("groupId").GetGuid());
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            Assert.Equal(1, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, newcomer.UserId));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, newcomer.UserId, "JoinedViaInvite"));
        }

        [Fact]
        public async Task Join_CurrentMember_Answers200WithTheGroupIdAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(setup.Member.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(setup.GroupId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("groupId").GetGuid());
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, setup.Member.UserId, "JoinedViaInvite"));
        }

        [Fact]
        public async Task Join_PersonWhoLeft_ComesBackOnTheSameRowWithTheRemovedFieldsCleared()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Left");
            int rowsBefore = await GroupRows.CountMembersAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(setup.Member.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(setup.GroupId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("groupId").GetGuid());
            Assert.Equal(rowsBefore, await GroupRows.CountMembersAsync(_app, setup.GroupId));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_by_user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "end_kind"));
        }

        [Fact]
        public async Task Join_PersonWhoLeft_WritesOneMemberJoinedEventAndOneJoinedViaInviteUsageEvent()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Left");

            await GroupRequests.JoinAsync(setup.Member.Client, setup.InviteToken);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberJoined"));
            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberJoined", "actor_user_id"));
            Assert.Equal(setup.MemberRowId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberJoined", "member_id"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, setup.Member.UserId, "JoinedViaInvite"));
        }

        [Fact]
        public async Task Join_RemovedPerson_Answers403InviteRemovedAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(setup.Member.Client, setup.InviteToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.INVITE_REMOVED);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, setup.Member.UserId, "JoinedViaInvite"));
        }

        [Fact]
        public async Task Join_Anonymous_Answers401AndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsync(_app.CreateClient(), setup.InviteToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            HttpResponseMessage signedIn = await GroupRequests.JoinAsync(setup.Member.Client, setup.InviteToken);
            Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
        }

        [Fact]
        public async Task Join_UnknownToken_Answers404InviteNotFound()
        {
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, "no-such-token");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, newcomer.UserId, "JoinedViaInvite"));
        }

        [Fact]
        public async Task Join_PreviousTokenAfterAReset_Answers404InviteNotFoundAndAddsNoRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            await GroupRows.SetPreviousInviteTokenAsync(_app, setup.GroupId, "the-old-token");

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, "the-old-token");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
            Assert.Equal(0, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, newcomer.UserId));
        }

        [Fact]
        public async Task Join_DeletedGroup_Answers404InviteNotFoundAndAddsNoRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");
            await GroupRows.SetDeletedAsync(_app, setup.GroupId, setup.Owner.UserId, 0);

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
            Assert.Equal(0, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, newcomer.UserId));
        }

        [Fact]
        public async Task Join_AccountWhoseNameEqualsAPlainName_JoinsWithoutClaimingAndLeavesThePlainNameAlone()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Cvetan", setup.Owner.UserId);
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, newcomer.UserId));
            Assert.NotEqual(plainId, await GroupRows.CurrentMemberIdAsync(_app, setup.GroupId, newcomer.UserId));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "claimed_at"));
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"token\":null}")]
        [InlineData("")]
        public async Task Join_MissingTokenOrEmptyBody_Answers400(string json)
        {
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(newcomer.Client, GroupRequests.JoinPath, json);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Join_BlankToken_Answers400OrInviteNotFound(string token)
        {
            SignedInUser newcomer = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.JoinAsync(newcomer.Client, token);

            await BlankTokenAnswer.AssertRefusedAsync(response);
        }
    }
}
