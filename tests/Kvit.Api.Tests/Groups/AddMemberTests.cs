using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class AddMemberTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Add_ValidName_Answers200WithTheNewRowInTheMemberShape()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, "Marko");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(GroupRequests.MemberFieldNames, GroupRequests.PropertyNamesOf(body));
            Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
            Assert.Equal("Marko", body.GetProperty("name").GetString());
            Assert.Equal("Marko", body.GetProperty("displayName").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("userId").ValueKind);
            Assert.True(body.GetProperty("isNameOnly").GetBoolean());
            Assert.False(body.GetProperty("isOwner").GetBoolean());
            Assert.False(body.GetProperty("isYou").GetBoolean());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("claimedName").ValueKind);
        }

        [Fact]
        public async Task Add_ValidName_StoresAPlainNameRowAddedByTheCaller()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, "Marko"));

            Guid memberId = body.GetProperty("id").GetGuid();
            Assert.Equal(setup.GroupId.ToString(), await GroupRows.MemberRowValueAsync(_app, memberId, "group_id"));
            Assert.Equal("Marko", await GroupRows.MemberRowValueAsync(_app, memberId, "name"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, memberId, "user_id"));
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, memberId, "added_by_user_id"));
            Assert.Equal("true", await GroupRows.MemberRowValueAsync(_app, memberId, "joined_at > now() - interval '1 minute'"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, memberId, "claimed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, memberId, "removed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, memberId, "end_kind"));
        }

        [Fact]
        public async Task Add_ValidName_WritesOneMemberAddedEventByTheCallerAboutTheNewRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, "Marko"));

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberAdded"));
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberAdded", "actor_user_id"));
            Assert.Equal(body.GetProperty("id").GetGuid().ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberAdded", "member_id"));
        }

        [Fact]
        public async Task Add_ValidName_ShowsInTheMembersListAndRaisesTheMemberCount()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            int countBefore = await GroupRequests.MemberCountOfAsync(setup.Owner.Client, setup.GroupId);

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, "Marko"));

            Assert.Contains(body.GetProperty("id").GetGuid(), await GroupRequests.MemberIdsAsync(setup.Owner.Client, setup.GroupId));
            Assert.Equal(countBefore + 1, await GroupRequests.MemberCountOfAsync(setup.Owner.Client, setup.GroupId));
        }

        [Fact]
        public async Task Add_MemberWhoIsNotTheOwner_Answers200()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Member.Client, setup.GroupId, "Marko");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, body.GetProperty("id").GetGuid(), "added_by_user_id"));
        }

        [Fact]
        public async Task Add_NameWithSurroundingSpaces_StoresAndAnswersTheTrimmedName()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, "  Marko  "));

            Assert.Equal("Marko", body.GetProperty("name").GetString());
            Assert.Equal("Marko", await GroupRows.MemberRowValueAsync(_app, body.GetProperty("id").GetGuid(), "name"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(60)]
        public async Task Add_NameAtTheLengthLimits_Answers200(int nameLength)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, new string('a', nameLength));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Add_EmptyOrWhitespaceName_Answers400NameInvalidAndWritesNothing(string name)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, name);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_INVALID);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Add_NameOf61Characters_Answers400NameInvalidAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, new string('a', 61));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_INVALID);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Theory]
        [InlineData("Marko")]
        [InlineData("marko")]
        [InlineData("MARKO")]
        [InlineData("  marko  ")]
        public async Task Add_NameOfAPlainNameInAnyLetterCase_Answers400NameTakenAndWritesNothing(string name)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, name);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Theory]
        [InlineData("Bojan")]
        [InlineData("bojan")]
        [InlineData("BOJAN")]
        public async Task Add_NameOfAnAccountMembersDisplayNameInAnyLetterCase_Answers400NameTaken(string name)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, name);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
        }

        [Fact]
        public async Task Add_NameOfTheOwnersDisplayName_Answers400NameTaken()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Member.Client, setup.GroupId, GroupsApp.OwnerName);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
        }

        [Fact]
        public async Task Add_CurrentNameOfARenamedAccount_Answers400NameTaken()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetDisplayNameAsync(_app, setup.Member.UserId, "Zoran");

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, "zoran");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
        }

        [Fact]
        public async Task Add_OldStoredNameOfARenamedAccount_Answers200BecauseNobodyShowsItAnyMore()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetDisplayNameAsync(_app, setup.Member.UserId, "Zoran");

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, GroupsApp.MemberName);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        [InlineData("SetAside")]
        public async Task Add_NameOfAnAccountMemberWhoIsNoLongerCurrent_Answers200(string endKind)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, endKind);

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, GroupsApp.MemberName);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Add_NameOfARemovedPlainName_Answers200()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, "Marko");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Add_SameNameInAnotherGroup_Answers200()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(setup.Owner);
            await GroupRows.InsertMemberAsync(_app, otherGroupId, null, "Marko", setup.Owner.UserId);

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, "Marko");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Add_NonMember_Answers404GroupNotFoundBeforeLookingAtTheNameAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(stranger.Client, setup.GroupId, "");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }
    }
}
