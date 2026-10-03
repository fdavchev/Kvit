using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class InvitePreviewTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Preview_AnonymousCaller_Answers200OpenWithTheGroupNameAndEmoji()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.PreviewAsync(_app.CreateClient(), setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Open", body.GetProperty("status").GetString());
            Assert.Equal(GroupRows.DefaultName, body.GetProperty("name").GetString());
            Assert.Equal(GroupRows.DefaultEmoji, body.GetProperty("emoji").GetString());
        }

        [Fact]
        public async Task Preview_AnonymousCaller_AnswersTheFieldsAndNoGroupId()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.PreviewAsync(_app.CreateClient(), setup.InviteToken));

            string[] namesWithoutGroupId = [.. GroupRequests.PropertyNamesOf(body).Where(name => name != "groupId")];
            Assert.Equal(["emoji", "memberNames", "name", "status", "unclaimedNames"], namesWithoutGroupId);
            AssertNoGroupId(body);
        }

        [Fact]
        public async Task Preview_Group_AnswersTheDisplayNamesOfAllCurrentMembersAndNoOneElse()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Removed one", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Left one", setup.Owner.UserId, DateTimeOffset.UtcNow, "Left");
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Set aside one", setup.Owner.UserId, DateTimeOffset.UtcNow, "SetAside");
            await UserRows.SetDisplayNameAsync(_app, setup.Member.UserId, "Zoran");

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.PreviewAsync(_app.CreateClient(), setup.InviteToken));

            Assert.Equal([GroupsApp.OwnerName, "Marko", "Zoran"], GroupRequests.StringsOf(body.GetProperty("memberNames")));
        }

        [Fact]
        public async Task Preview_Group_AnswersTheCurrentPlainNamesWithTheirIdsAsUnclaimedNames()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid markoId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            Guid zoraId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Zora", setup.Owner.UserId);
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Removed one", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.PreviewAsync(_app.CreateClient(), setup.InviteToken));

            JsonElement unclaimed = body.GetProperty("unclaimedNames");
            Assert.Equal(new[] { markoId, zoraId }.Order(), GroupRequests.IdsOf(unclaimed).Order());
            Assert.Equal(["id", "name"], GroupRequests.PropertyNamesOf(unclaimed[0]));
            Assert.Equal("Marko", GroupRequests.RowWithId(unclaimed, markoId).GetProperty("name").GetString());
            Assert.Equal("Zora", GroupRequests.RowWithId(unclaimed, zoraId).GetProperty("name").GetString());
        }

        [Fact]
        public async Task Preview_GroupWithAClaimedName_DoesNotListTheClaimedNameAsUnclaimed()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await _app.SetUpClaimedNameAsync(setup, "Marko");

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.PreviewAsync(_app.CreateClient(), setup.InviteToken));

            JsonElement unclaimed = body.GetProperty("unclaimedNames");
            Assert.Equal(JsonValueKind.Array, unclaimed.ValueKind);
            Assert.Empty(unclaimed.EnumerateArray());
            Assert.Equal([GroupsApp.OwnerName, GroupsApp.MemberName], GroupRequests.StringsOf(body.GetProperty("memberNames")));
        }

        [Fact]
        public async Task Preview_SignedInNonMember_Answers200OpenWithNoGroupId()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupRequests.PreviewAsync(stranger.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Open", body.GetProperty("status").GetString());
            AssertNoGroupId(body);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Preview_CurrentMember_Answers200AlreadyMemberWithTheGroupId(bool isOwner)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            HttpClient client = isOwner ? setup.Owner.Client : setup.Member.Client;

            HttpResponseMessage response = await GroupRequests.PreviewAsync(client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("AlreadyMember", body.GetProperty("status").GetString());
            Assert.Equal(setup.GroupId, body.GetProperty("groupId").GetGuid());
        }

        [Fact]
        public async Task Preview_RemovedPerson_Answers200RemovedWithNoGroupId()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");

            HttpResponseMessage response = await GroupRequests.PreviewAsync(setup.Member.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Removed", body.GetProperty("status").GetString());
            AssertNoGroupId(body);
        }

        [Fact]
        public async Task Preview_PersonWhoLeft_Answers200OpenWithNoGroupId()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Left");

            HttpResponseMessage response = await GroupRequests.PreviewAsync(setup.Member.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Open", body.GetProperty("status").GetString());
            AssertNoGroupId(body);
        }

        [Fact]
        public async Task Preview_UnknownTokenFromAnAnonymousCaller_Answers404InviteNotFound()
        {
            HttpResponseMessage response = await GroupRequests.PreviewAsync(_app.CreateClient(), "no-such-token");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }

        [Fact]
        public async Task Preview_UnknownTokenFromASignedInCaller_Answers404InviteNotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.PreviewAsync(user.Client, "no-such-token");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }

        [Fact]
        public async Task Preview_PreviousTokenAfterAReset_Answers404InviteNotFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.SetPreviousInviteTokenAsync(_app, setup.GroupId, "the-old-token");

            HttpResponseMessage response = await GroupRequests.PreviewAsync(_app.CreateClient(), "the-old-token");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }

        [Fact]
        public async Task Preview_DeletedGroup_Answers404InviteNotFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.SetDeletedAsync(_app, setup.GroupId, setup.Owner.UserId, 0);

            HttpResponseMessage response = await GroupRequests.PreviewAsync(_app.CreateClient(), setup.InviteToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }

        [Fact]
        public async Task Preview_SignedInNonMember_WritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.PreviewAsync(stranger.Client, setup.InviteToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            Assert.Equal(0, await GroupRows.CountMemberRowsOfUserAsync(_app, setup.GroupId, stranger.UserId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, stranger.UserId, "JoinedViaInvite"));
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"token\":null}")]
        [InlineData("")]
        public async Task Preview_MissingTokenOrEmptyBody_Answers400(string json)
        {
            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(_app.CreateClient(), GroupRequests.PreviewPath, json);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Preview_BlankToken_Answers400OrInviteNotFound(string token)
        {
            HttpResponseMessage response = await GroupRequests.PreviewAsync(_app.CreateClient(), token);

            await BlankTokenAnswer.AssertRefusedAsync(response);
        }

        private static void AssertNoGroupId(JsonElement body)
        {
            if (body.TryGetProperty("groupId", out JsonElement groupId))
            {
                Assert.Equal(JsonValueKind.Null, groupId.ValueKind);
            }
        }
    }
}
