using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class CreateGroupTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Create_ValidRequest_AnswersExactlyTheTenGroupFields()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            string[] propertyNames = [.. body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
            string[] expectedNames = ["defaultCurrency", "emoji", "id", "inviteToken", "isOwner", "kind", "memberCount", "name", "ownerUserId", "status"];
            Assert.Equal(expectedNames, propertyNames);
        }

        [Fact]
        public async Task Create_ValidRequest_AnswersTheRequestedValuesAndTheOwnerState()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(user.Client, "Dinner", "\U0001F37D", "EUR");

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
            Assert.Equal("Group", body.GetProperty("kind").GetString());
            Assert.Equal("Dinner", body.GetProperty("name").GetString());
            Assert.Equal("\U0001F37D", body.GetProperty("emoji").GetString());
            Assert.Equal("EUR", body.GetProperty("defaultCurrency").GetString());
            Assert.Equal("Open", body.GetProperty("status").GetString());
            Assert.Equal(user.UserId, body.GetProperty("ownerUserId").GetGuid());
            Assert.True(body.GetProperty("isOwner").GetBoolean());
            Assert.Equal(1, body.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Create_ValidRequest_AnswersAnInviteTokenOf43Base64UrlCharacters()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(user.Client);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Matches(GroupRequests.TokenFormat, body.GetProperty("inviteToken").GetString());
        }

        [Fact]
        public async Task Create_TwoGroups_GetDifferentInviteTokens()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement first = await AuthRequests.ReadJsonAsync(await GroupRequests.CreateAsync(user.Client));
            JsonElement second = await AuthRequests.ReadJsonAsync(await GroupRequests.CreateAsync(user.Client));

            Assert.NotEqual(first.GetProperty("inviteToken").GetString(), second.GetProperty("inviteToken").GetString());
            Assert.NotEqual(first.GetProperty("id").GetGuid(), second.GetProperty("id").GetGuid());
        }

        [Fact]
        public async Task Create_ValidRequest_StoresTheGroupRow()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.CreateAsync(user.Client, "Dinner", "\U0001F37D", "EUR"));

            Guid groupId = body.GetProperty("id").GetGuid();
            Assert.Equal("Group", await GroupRows.GroupValueAsync(_app, groupId, "kind"));
            Assert.Equal("Dinner", await GroupRows.GroupValueAsync(_app, groupId, "name"));
            Assert.Equal("\U0001F37D", await GroupRows.GroupValueAsync(_app, groupId, "emoji"));
            Assert.Equal("EUR", await GroupRows.GroupValueAsync(_app, groupId, "default_currency"));
            Assert.Equal("Open", await GroupRows.GroupValueAsync(_app, groupId, "status"));
            Assert.Equal(user.UserId.ToString(), await GroupRows.GroupValueAsync(_app, groupId, "owner_user_id"));
            Assert.Equal(user.UserId.ToString(), await GroupRows.GroupValueAsync(_app, groupId, "created_by_user_id"));
            Assert.Null(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
            Assert.Equal(body.GetProperty("inviteToken").GetString(), await GroupRows.GroupValueAsync(_app, groupId, "invite_token"));
        }

        [Fact]
        public async Task Create_ValidRequest_AddsTheOwnerAsTheOnlyActiveMember()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.CreateAsync(user.Client));

            Guid groupId = body.GetProperty("id").GetGuid();
            Assert.Equal(1, await GroupRows.CountMembersAsync(_app, groupId));
            Assert.Equal(user.Form.DisplayName, await GroupRows.MemberValueAsync(_app, groupId, user.UserId, "name"));
            Assert.Null(await GroupRows.MemberValueAsync(_app, groupId, user.UserId, "claimed_at"));
            Assert.Null(await GroupRows.MemberValueAsync(_app, groupId, user.UserId, "removed_at"));
            Assert.Null(await GroupRows.MemberValueAsync(_app, groupId, user.UserId, "end_kind"));
        }

        [Fact]
        public async Task Create_ValidRequest_SetsTheOwnersJoinedAtToNow()
        {
            SignedInUser user = await _app.SignUpAsync();
            DateTimeOffset before = DateTimeOffset.UtcNow;

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.CreateAsync(user.Client));

            DateTimeOffset after = DateTimeOffset.UtcNow;
            Guid groupId = body.GetProperty("id").GetGuid();
            string? epochSeconds = await GroupRows.MemberValueAsync(_app, groupId, user.UserId, "extract(epoch FROM joined_at)");
            DateTimeOffset joinedAt = DateTimeOffset.FromUnixTimeMilliseconds((long)(double.Parse(epochSeconds!, CultureInfo.InvariantCulture) * 1000));
            Assert.InRange(joinedAt, before.AddSeconds(-5), after.AddSeconds(5));
        }

        [Fact]
        public async Task Create_ValidRequest_WritesOneGroupCreatedActivityEventByTheCaller()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.CreateAsync(user.Client));

            Guid groupId = body.GetProperty("id").GetGuid();
            Assert.Equal(1, await GroupRows.CountEventsAsync(_app, groupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "GroupCreated"));
            Assert.Equal(user.UserId.ToString(), await GroupRows.EventValueAsync(_app, groupId, "GroupCreated", "actor_user_id"));
        }

        [Fact]
        public async Task Create_ValidRequest_WritesOneGroupCreatedUsageEventForTheCaller()
        {
            SignedInUser user = await _app.SignUpAsync();

            await GroupRequests.CreateAsync(user.Client);

            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, user.UserId, "GroupCreated"));
        }

        [Fact]
        public async Task Create_NameWithSurroundingSpaces_StoresAndAnswersTheTrimmedName()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(user.Client, "  Greece trip  ");

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Greece trip", body.GetProperty("name").GetString());
            Assert.Equal("Greece trip", await GroupRows.GroupValueAsync(_app, body.GetProperty("id").GetGuid(), "name"));
        }

        [Theory]
        [InlineData(60, "EUR")]
        [InlineData(1, "MKD")]
        public async Task Create_NameAtTheLengthLimits_Answers200(int nameLength, string currency)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(user.Client, new string('a', nameLength), currency: currency);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Create_EmojiOf32Characters_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(user.Client, emoji: new string('a', 32));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(InvalidGroupInput.Cases), MemberType = typeof(InvalidGroupInput))]
        public async Task Create_InvalidInput_Answers400WithTheErrorCodeAndCreatesNothing(string name, string emoji, string currency, string expectedErrorCode)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(user.Client, name, emoji, currency);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(0, await GroupRows.CountGroupsOwnedByAsync(_app, user.UserId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, user.UserId, "GroupCreated"));
        }

        [Fact]
        public async Task Create_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await GroupRequests.CreateAsync(client);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
