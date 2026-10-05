using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class RestoreGroupTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(29)]
        public async Task Restore_Owner_WithinThe30Days_Answers204AndClearsTheDeletion(int deletedDaysAgo)
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, deletedDaysAgo);

            HttpResponseMessage response = await GroupRequests.RestoreAsync(owner.Client, groupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
            Assert.Null(await GroupRows.GroupValueAsync(_app, groupId, "deleted_by_user_id"));
        }

        [Fact]
        public async Task Restore_GroupDeleted29DaysAnd23HoursAgo_Answers204()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAgoAsync(_app, groupId, owner.UserId, TimeSpan.FromDays(29) + TimeSpan.FromHours(23));

            HttpResponseMessage response = await GroupRequests.RestoreAsync(owner.Client, groupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
        }

        [Fact]
        public async Task Restore_GroupDeleted30DaysAnd1MinuteAgo_Answers400GroupRestoreExpired()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAgoAsync(_app, groupId, owner.UserId, TimeSpan.FromDays(30) + TimeSpan.FromMinutes(1));

            HttpResponseMessage response = await GroupRequests.RestoreAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_RESTORE_EXPIRED);
            Assert.NotNull(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
        }

        [Fact]
        public async Task Restore_Owner_WritesOneGroupRestoredEventByTheOwner()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 1);

            await GroupRequests.RestoreAsync(owner.Client, groupId);

            Assert.Equal(1, await GroupRows.CountEventsAsync(_app, groupId));
            Assert.Equal(owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, groupId, "GroupRestored", "actor_user_id"));
        }

        [Fact]
        public async Task Restore_Owner_MakesTheGroupVisibleAgainInGetAndInTheGroupsList()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 1);

            await GroupRequests.RestoreAsync(owner.Client, groupId);

            HttpResponseMessage get = await GroupRequests.GetAsync(owner.Client, groupId);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            JsonElement list = await AuthRequests.ReadJsonAsync(await GroupRequests.ListAsync(owner.Client));
            Assert.Equal(groupId, Assert.Single(GroupRequests.IdsOf(list.GetProperty("groups"))));
            Assert.Empty(GroupRequests.IdsOf(list.GetProperty("recentlyDeleted")));
        }

        [Fact]
        public async Task Restore_GroupDeletedThroughTheApi_BringsItBack()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRequests.DeleteAsync(owner.Client, groupId);

            HttpResponseMessage response = await GroupRequests.RestoreAsync(owner.Client, groupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(owner.Client, groupId)).StatusCode);
        }

        [Fact]
        public async Task Restore_GroupThatIsNotDeleted_Answers400GroupNotDeletedAndWritesNoEvent()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.RestoreAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_DELETED);
            Assert.Equal(0, await GroupRows.CountEventsAsync(_app, groupId));
        }

        [Fact]
        public async Task Restore_GroupDeleted31DaysAgo_Answers400GroupRestoreExpiredAndKeepsItDeleted()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 31);

            HttpResponseMessage response = await GroupRequests.RestoreAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_RESTORE_EXPIRED);
            Assert.NotNull(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
            Assert.Equal(0, await GroupRows.CountEventsAsync(_app, groupId));
        }

        [Fact]
        public async Task Restore_MemberWhoIsNotTheOwner_Answers403GroupNotOwnerAndKeepsItDeleted()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 1);

            HttpResponseMessage response = await GroupRequests.RestoreAsync(member.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.NotNull(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
        }

        [Fact]
        public async Task Restore_NonMember_Answers404GroupNotFoundAndKeepsItDeleted()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser stranger = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 1);

            HttpResponseMessage response = await GroupRequests.RestoreAsync(stranger.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.NotNull(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
        }

        [Fact]
        public async Task Restore_UnknownGroupId_Answers404GroupNotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.RestoreAsync(user.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Restore_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await GroupRequests.RestoreAsync(client, Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
