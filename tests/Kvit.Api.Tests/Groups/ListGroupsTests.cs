using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class ListGroupsTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        private const int DeletedGroupRestoreDays = 30;

        [Fact]
        public async Task List_NewUser_AnswersExactlyThreeEmptyArrays()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.ListAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            string[] propertyNames = [.. body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
            string[] expectedNames = ["finishedGroups", "groups", "recentlyDeleted"];
            Assert.Equal(expectedNames, propertyNames);
            Assert.Equal(0, body.GetProperty("groups").GetArrayLength());
            Assert.Equal(0, body.GetProperty("finishedGroups").GetArrayLength());
            Assert.Equal(0, body.GetProperty("recentlyDeleted").GetArrayLength());
        }

        [Fact]
        public async Task List_GroupOfTheCaller_AnswersItsRowFields()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, user.UserId, "Dinner", "EUR");

            JsonElement body = await ListAsync(user);

            JsonElement row = GroupRequests.RowWithId(body.GetProperty("groups"), groupId);
            Assert.Equal("Dinner", row.GetProperty("name").GetString());
            Assert.Equal(GroupRows.DefaultEmoji, row.GetProperty("emoji").GetString());
            Assert.Equal("EUR", row.GetProperty("defaultCurrency").GetString());
            Assert.Equal(1, row.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task List_GroupCreatedThroughTheApi_IsInGroups()
        {
            SignedInUser user = await _app.SignUpAsync();
            JsonElement created = await AuthRequests.ReadJsonAsync(await GroupRequests.CreateAsync(user.Client));

            JsonElement body = await ListAsync(user);

            Assert.Contains(created.GetProperty("id").GetGuid(), GroupRequests.IdsOf(body.GetProperty("groups")));
        }

        [Fact]
        public async Task List_TwoUsers_EachSeeOnlyTheirOwnGroups()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync();
            Guid firstGroupId = await GroupRows.InsertGroupWithOwnerAsync(_app, first.UserId);
            Guid secondGroupId = await GroupRows.InsertGroupWithOwnerAsync(_app, second.UserId);

            JsonElement firstBody = await ListAsync(first);
            JsonElement secondBody = await ListAsync(second);

            Assert.Equal([firstGroupId], GroupRequests.IdsOf(firstBody.GetProperty("groups")));
            Assert.Equal([secondGroupId], GroupRequests.IdsOf(secondBody.GetProperty("groups")));
        }

        [Fact]
        public async Task List_GroupWhereTheCallerIsAMember_IsInGroupsWithoutBeingOwner()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId);

            JsonElement body = await ListAsync(member);

            Assert.Equal([groupId], GroupRequests.IdsOf(body.GetProperty("groups")));
        }

        [Fact]
        public async Task List_MemberCount_CountsOnlyTheMembersCurrentlyInTheGroup()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, null, "Marko", owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, null, "Grandma", owner.UserId, DateTimeOffset.UtcNow, "Removed");

            JsonElement body = await ListAsync(owner);

            JsonElement row = GroupRequests.RowWithId(body.GetProperty("groups"), groupId);
            Assert.Equal(3, row.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task List_MemberWhoWasRemoved_DoesNotSeeTheGroup()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId, DateTimeOffset.UtcNow, "Removed");

            JsonElement body = await ListAsync(member);

            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("groups")));
        }

        [Fact]
        public async Task List_FinishedGroup_IsInFinishedGroupsAndNotInGroups()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, user.UserId, status: "Finished");

            JsonElement body = await ListAsync(user);

            Assert.Equal([groupId], GroupRequests.IdsOf(body.GetProperty("finishedGroups")));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("groups")));
        }

        [Fact]
        public async Task List_ClosingGroup_IsStillInGroups()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, user.UserId, status: "Closing");

            JsonElement body = await ListAsync(user);

            Assert.Equal([groupId], GroupRequests.IdsOf(body.GetProperty("groups")));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("finishedGroups")));
        }

        [Fact]
        public async Task List_DeletedGroupOfTheOwner_IsInRecentlyDeletedAndNotInGroups()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 0);

            JsonElement body = await ListAsync(owner);

            Assert.Equal([groupId], GroupRequests.IdsOf(body.GetProperty("recentlyDeleted")));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("groups")));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("finishedGroups")));
        }

        [Fact]
        public async Task List_RecentlyDeletedRow_HasDeletedAtAndRestorableUntil30DaysLater()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 2);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            JsonElement body = await ListAsync(owner);

            JsonElement row = GroupRequests.RowWithId(body.GetProperty("recentlyDeleted"), groupId);
            DateTimeOffset deletedAt = row.GetProperty("deletedAt").GetDateTimeOffset();
            DateTimeOffset restorableUntil = row.GetProperty("restorableUntil").GetDateTimeOffset();
            Assert.InRange(deletedAt, now.AddDays(-2).AddMinutes(-5), now.AddDays(-2).AddMinutes(5));
            Assert.Equal(deletedAt.AddDays(DeletedGroupRestoreDays), restorableUntil);
            Assert.Equal(GroupRows.DefaultName, row.GetProperty("name").GetString());
        }

        [Fact]
        public async Task List_GroupDeleted29DaysAnd23HoursAgo_IsStillInRecentlyDeleted()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAgoAsync(_app, groupId, owner.UserId, TimeSpan.FromDays(29) + TimeSpan.FromHours(23));

            JsonElement body = await ListAsync(owner);

            Assert.Equal([groupId], GroupRequests.IdsOf(body.GetProperty("recentlyDeleted")));
        }

        [Fact]
        public async Task List_GroupDeleted30DaysAnd1MinuteAgo_IsNotListedAnywhere()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAgoAsync(_app, groupId, owner.UserId, TimeSpan.FromDays(30) + TimeSpan.FromMinutes(1));

            JsonElement body = await ListAsync(owner);

            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("recentlyDeleted")));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("groups")));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("finishedGroups")));
        }

        [Fact]
        public async Task List_GroupDeleted29DaysAgo_IsStillInRecentlyDeleted()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 29);

            JsonElement body = await ListAsync(owner);

            Assert.Equal([groupId], GroupRequests.IdsOf(body.GetProperty("recentlyDeleted")));
        }

        [Fact]
        public async Task List_GroupDeleted31DaysAgo_IsNotListedAnywhere()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 31);

            JsonElement body = await ListAsync(owner);

            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("recentlyDeleted")));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("groups")));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("finishedGroups")));
        }

        [Fact]
        public async Task List_DeletedGroupWhereTheCallerIsOnlyAMember_IsNotListedAnywhere()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 1);

            JsonElement body = await ListAsync(member);

            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("recentlyDeleted")));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("groups")));
        }

        [Fact]
        public async Task List_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await GroupRequests.ListAsync(client);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static async Task<JsonElement> ListAsync(SignedInUser user)
        {
            HttpResponseMessage response = await GroupRequests.ListAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }
    }
}
