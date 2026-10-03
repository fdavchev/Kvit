using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class GetGroupTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Get_Owner_AnswersExactlyTheTenGroupFields()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.GetAsync(owner.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            string[] propertyNames = [.. body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
            string[] expectedNames = ["defaultCurrency", "emoji", "id", "inviteToken", "isOwner", "kind", "memberCount", "name", "ownerUserId", "status"];
            Assert.Equal(expectedNames, propertyNames);
        }

        [Fact]
        public async Task Get_Owner_AnswersTheStoredValues()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId, "Dinner", "EUR");
            string? storedToken = await GroupRows.GroupValueAsync(_app, groupId, "invite_token");

            HttpResponseMessage response = await GroupRequests.GetAsync(owner.Client, groupId);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(groupId, body.GetProperty("id").GetGuid());
            Assert.Equal("Group", body.GetProperty("kind").GetString());
            Assert.Equal("Dinner", body.GetProperty("name").GetString());
            Assert.Equal(GroupRows.DefaultEmoji, body.GetProperty("emoji").GetString());
            Assert.Equal("EUR", body.GetProperty("defaultCurrency").GetString());
            Assert.Equal("Open", body.GetProperty("status").GetString());
            Assert.Equal(owner.UserId, body.GetProperty("ownerUserId").GetGuid());
            Assert.True(body.GetProperty("isOwner").GetBoolean());
            Assert.Equal(1, body.GetProperty("memberCount").GetInt32());
            Assert.Equal(storedToken, body.GetProperty("inviteToken").GetString());
        }

        [Fact]
        public async Task Get_MemberWhoIsNotTheOwner_SeesIsOwnerFalseTheOwnerIdAndTheInviteToken()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId);
            string? storedToken = await GroupRows.GroupValueAsync(_app, groupId, "invite_token");

            HttpResponseMessage response = await GroupRequests.GetAsync(member.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.False(body.GetProperty("isOwner").GetBoolean());
            Assert.Equal(owner.UserId, body.GetProperty("ownerUserId").GetGuid());
            Assert.Equal(storedToken, body.GetProperty("inviteToken").GetString());
            Assert.Equal(2, body.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_GroupCreatedThroughTheApi_AnswersTheSameGroup()
        {
            SignedInUser owner = await _app.SignUpAsync();
            JsonElement created = await AuthRequests.ReadJsonAsync(await GroupRequests.CreateAsync(owner.Client));
            Guid groupId = created.GetProperty("id").GetGuid();

            HttpResponseMessage response = await GroupRequests.GetAsync(owner.Client, groupId);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(created.GetProperty("inviteToken").GetString(), body.GetProperty("inviteToken").GetString());
            Assert.Equal(groupId, body.GetProperty("id").GetGuid());
        }

        [Fact]
        public async Task Get_NonMember_Answers404GroupNotFound()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser stranger = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.GetAsync(stranger.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Get_UnknownGroupId_Answers404GroupNotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.GetAsync(user.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Get_DeletedGroup_Answers404GroupNotFoundEvenForTheOwner()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 0);

            HttpResponseMessage response = await GroupRequests.GetAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Get_MemberWhoWasRemoved_Answers404GroupNotFound()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId);
            await GroupRows.EndMembershipAsync(_app, groupId, member.UserId, "Removed");

            HttpResponseMessage response = await GroupRequests.GetAsync(member.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Get_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await GroupRequests.GetAsync(client, Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
