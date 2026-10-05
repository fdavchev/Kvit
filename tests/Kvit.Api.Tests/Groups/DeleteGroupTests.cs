using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class DeleteGroupTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Delete_Owner_Answers204AndSetsDeletedAtAndDeletedBy()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(owner.Client, groupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.NotNull(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
            Assert.Equal(owner.UserId.ToString(), await GroupRows.GroupValueAsync(_app, groupId, "deleted_by_user_id"));
        }

        [Fact]
        public async Task Delete_Owner_WritesOneGroupDeletedEventByTheOwner()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            await GroupRequests.DeleteAsync(owner.Client, groupId);

            Assert.Equal(1, await GroupRows.CountEventsAsync(_app, groupId));
            Assert.Equal(owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, groupId, "GroupDeleted", "actor_user_id"));
        }

        [Fact]
        public async Task Delete_Owner_MovesTheGroupFromGroupsToRecentlyDeleted()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            await GroupRequests.DeleteAsync(owner.Client, groupId);

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.ListAsync(owner.Client));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("groups")));
            Assert.Equal(groupId, Assert.Single(GroupRequests.IdsOf(body.GetProperty("recentlyDeleted"))));
        }

        [Fact]
        public async Task Delete_Owner_MakesGetAnswer404()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRequests.DeleteAsync(owner.Client, groupId);

            HttpResponseMessage response = await GroupRequests.GetAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Delete_MemberWhoIsNotTheOwner_Answers403GroupNotOwnerAndDeletesNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(member.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Null(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
            Assert.Equal(0, await GroupRows.CountEventsAsync(_app, groupId));
        }

        [Fact]
        public async Task Delete_NonMember_Answers404GroupNotFoundAndDeletesNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser stranger = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.DeleteAsync(stranger.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Null(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
        }

        [Fact]
        public async Task Delete_UnknownGroupId_Answers404GroupNotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.DeleteAsync(user.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Delete_TwiceInARow_Answers404GroupNotFoundTheSecondTimeAndWritesOneEvent()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRequests.DeleteAsync(owner.Client, groupId);

            HttpResponseMessage second = await GroupRequests.DeleteAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(second, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "GroupDeleted"));
        }

        [Fact]
        public async Task Delete_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await GroupRequests.DeleteAsync(client, Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
