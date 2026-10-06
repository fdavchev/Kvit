using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class ListMembersTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task List_Member_AnswersExactlyMembersRemovedAndCanClaimNames()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);

            HttpResponseMessage response = await GroupRequests.ListMembersAsync(owner.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["canClaimNames", "members", "removed"], GroupRequests.PropertyNamesOf(body));
        }

        [Fact]
        public async Task List_MemberRow_AnswersExactlyTheNineFields()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);

            JsonElement body = await GroupRequests.ReadMembersAsync(owner.Client, groupId);

            JsonElement row = Assert.Single(body.GetProperty("members").EnumerateArray());
            Assert.Equal(GroupRequests.MemberFieldNames, GroupRequests.PropertyNamesOf(row));
        }

        [Fact]
        public async Task List_OwnerRowSeenByTheOwner_IsOwnerAndIsYouWithTheAccount()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Owner.Client, setup.GroupId, setup.OwnerRowId);

            Assert.Equal(GroupsApp.OwnerName, row.GetProperty("name").GetString());
            Assert.Equal(GroupsApp.OwnerName, row.GetProperty("displayName").GetString());
            Assert.Equal(setup.Owner.UserId, row.GetProperty("userId").GetGuid());
            Assert.True(row.GetProperty("isOwner").GetBoolean());
            Assert.True(row.GetProperty("isYou").GetBoolean());
            Assert.False(row.GetProperty("isNameOnly").GetBoolean());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("claimedName").ValueKind);
        }

        [Fact]
        public async Task List_AccountMemberRowSeenByTheOwner_IsNeitherOwnerNorYou()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal(GroupsApp.MemberName, row.GetProperty("name").GetString());
            Assert.Equal(setup.Member.UserId, row.GetProperty("userId").GetGuid());
            Assert.False(row.GetProperty("isOwner").GetBoolean());
            Assert.False(row.GetProperty("isYou").GetBoolean());
            Assert.False(row.GetProperty("isNameOnly").GetBoolean());
        }

        [Fact]
        public async Task List_SeenByAMember_IsYouIsOnTheMembersOwnRowAndIsOwnerStaysOnTheOwnersRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Member.Client, setup.GroupId);

            JsonElement members = body.GetProperty("members");
            Assert.True(GroupRequests.RowWithId(members, setup.MemberRowId).GetProperty("isYou").GetBoolean());
            Assert.False(GroupRequests.RowWithId(members, setup.OwnerRowId).GetProperty("isYou").GetBoolean());
            Assert.True(GroupRequests.RowWithId(members, setup.OwnerRowId).GetProperty("isOwner").GetBoolean());
            Assert.False(GroupRequests.RowWithId(members, setup.MemberRowId).GetProperty("isOwner").GetBoolean());
        }

        [Fact]
        public async Task List_PlainName_IsNameOnlyWithNoUserAndTheNameAsDisplayName()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Owner.Client, setup.GroupId, plainId);

            Assert.Equal("Marko", row.GetProperty("name").GetString());
            Assert.Equal("Marko", row.GetProperty("displayName").GetString());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("userId").ValueKind);
            Assert.True(row.GetProperty("isNameOnly").GetBoolean());
            Assert.False(row.GetProperty("isOwner").GetBoolean());
            Assert.False(row.GetProperty("isYou").GetBoolean());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("claimedName").ValueKind);
        }

        [Fact]
        public async Task List_MembersAreOrderedByJoinedAtThenById()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);
            Guid ownerRowId = await GroupRows.CurrentMemberIdAsync(_app, groupId, owner.UserId);
            DateTimeOffset baseTime = DateTimeOffset.UtcNow.AddDays(-1);
            Guid third = await GroupRows.InsertMemberAsync(_app, groupId, null, "Third", owner.UserId, joinedAt: baseTime.AddHours(2));
            Guid first = await GroupRows.InsertMemberAsync(_app, groupId, null, "First", owner.UserId, joinedAt: baseTime);
            Guid second = await GroupRows.InsertMemberAsync(_app, groupId, null, "Second", owner.UserId, joinedAt: baseTime.AddHours(1));
            Guid tiedOne = await GroupRows.InsertMemberAsync(_app, groupId, null, "TiedOne", owner.UserId, joinedAt: baseTime.AddHours(3));
            Guid tiedTwo = await GroupRows.InsertMemberAsync(_app, groupId, null, "TiedTwo", owner.UserId, joinedAt: baseTime.AddHours(3));
            List<Guid> tiedByIdOrder = [.. new[] { tiedOne, tiedTwo }.OrderBy(id => id.ToString(), StringComparer.Ordinal)];

            List<Guid> ids = await GroupRequests.MemberIdsAsync(owner.Client, groupId);

            Assert.Equal([first, second, third, tiedByIdOrder[0], tiedByIdOrder[1], ownerRowId], ids);
        }

        [Fact]
        public async Task List_AccountRenamed_DisplayNameFollowsTheAccountWhileNameStaysTheStoredOne()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetDisplayNameAsync(_app, setup.Member.UserId, "Zoran");

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal("Zoran", row.GetProperty("displayName").GetString());
            Assert.Equal(GroupsApp.MemberName, row.GetProperty("name").GetString());
        }

        [Fact]
        public async Task List_ClaimedName_ShowsTheClaimedNameAndNoRowForTheSetAsideOne()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid claimedId = await _app.SetUpClaimedNameAsync(setup, "Marko");

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Member.Client, setup.GroupId);

            JsonElement members = body.GetProperty("members");
            JsonElement row = GroupRequests.RowWithId(members, claimedId);
            Assert.Equal("Marko", row.GetProperty("claimedName").GetString());
            Assert.Equal("Marko", row.GetProperty("name").GetString());
            Assert.Equal(GroupsApp.MemberName, row.GetProperty("displayName").GetString());
            Assert.Equal(setup.Member.UserId, row.GetProperty("userId").GetGuid());
            Assert.True(row.GetProperty("isYou").GetBoolean());
            Assert.False(row.GetProperty("isNameOnly").GetBoolean());
            Assert.DoesNotContain(setup.MemberRowId, GroupRequests.IdsOf(members));
        }

        [Fact]
        public async Task List_Owner_SeesRemovedMembersWithOnlyIdDisplayNameAndPictureUrlAndNotInMembers()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid removedPlainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Zora", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");
            await UserRows.SetDisplayNameAsync(_app, setup.Member.UserId, "Zoran");

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId);

            JsonElement removed = body.GetProperty("removed");
            Assert.Equal(new[] { setup.MemberRowId, removedPlainId }.Order(), GroupRequests.IdsOf(removed).Order());
            Assert.Equal(["displayName", "id", "pictureUrl"], GroupRequests.PropertyNamesOf(removed[0]));
            Assert.Equal("Zoran", GroupRequests.RowWithId(removed, setup.MemberRowId).GetProperty("displayName").GetString());
            Assert.Equal("Zora", GroupRequests.RowWithId(removed, removedPlainId).GetProperty("displayName").GetString());
            Assert.Equal([setup.OwnerRowId], GroupRequests.IdsOf(body.GetProperty("members")));
        }

        [Fact]
        public async Task List_MemberWhoIsNotTheOwner_SeesAnEmptyRemovedArray()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Zora", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Member.Client, setup.GroupId);

            JsonElement removed = body.GetProperty("removed");
            Assert.Equal(JsonValueKind.Array, removed.ValueKind);
            Assert.Empty(removed.EnumerateArray());
        }

        [Fact]
        public async Task List_LeftAndSetAsideRows_AreNeverShownNotEvenToTheOwner()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid leftId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Left one", setup.Owner.UserId, DateTimeOffset.UtcNow, "Left");
            Guid setAsideId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Set aside", setup.Owner.UserId, DateTimeOffset.UtcNow, "SetAside");

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId);

            List<Guid> shownIds = [.. GroupRequests.IdsOf(body.GetProperty("members")), .. GroupRequests.IdsOf(body.GetProperty("removed"))];
            Assert.DoesNotContain(leftId, shownIds);
            Assert.DoesNotContain(setAsideId, shownIds);
        }

        [Fact]
        public async Task List_MemberWithAnOrdinaryOwnRow_CanClaimNames()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Member.Client, setup.GroupId);

            Assert.True(body.GetProperty("canClaimNames").GetBoolean());
        }

        [Fact]
        public async Task List_OwnerWithAnOrdinaryOwnRow_CanClaimNames()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId);

            Assert.True(body.GetProperty("canClaimNames").GetBoolean());
        }

        [Fact]
        public async Task List_CallerWhoHoldsAClaimedName_CannotClaimNames()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await _app.SetUpClaimedNameAsync(setup, "Marko");

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Member.Client, setup.GroupId);

            Assert.False(body.GetProperty("canClaimNames").GetBoolean());
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        public async Task List_PersonWhoIsNoLongerAMember_Answers404GroupNotFound(string endKind)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, endKind);

            HttpResponseMessage response = await GroupRequests.ListMembersAsync(setup.Member.Client, setup.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }
    }
}
