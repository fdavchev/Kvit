using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class GroupListKindTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task List_GroupCreatedThroughTheApi_HasKindGroup()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(user);

            JsonElement row = await ListedRowAsync(user, groupId);

            Assert.Equal("Group", row.GetProperty("kind").GetString());
        }

        [Fact]
        public async Task List_OneBillGroup_HasKindOneBill()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupAsync(_app, user.UserId, kind: "OneBill");
            await GroupRows.InsertMemberAsync(_app, groupId, user.UserId, GroupRows.OwnerMemberName, user.UserId);

            JsonElement row = await ListedRowAsync(user, groupId);

            Assert.Equal("OneBill", row.GetProperty("kind").GetString());
        }

        [Fact]
        public async Task List_FinishedOneBillGroup_HasKindOneBillInTheFinishedList()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupAsync(_app, user.UserId, kind: "OneBill", status: "Finished");
            await GroupRows.InsertMemberAsync(_app, groupId, user.UserId, GroupRows.OwnerMemberName, user.UserId);

            HttpResponseMessage response = await GroupRequests.ListAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            JsonElement row = GroupRequests.RowWithId(body.GetProperty("finishedGroups"), groupId);
            Assert.Equal("OneBill", row.GetProperty("kind").GetString());
        }

        [Fact]
        public async Task List_BothKindsOfTheSameUser_EachRowCarriesItsOwnKind()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid plainGroup = await _app.CreateGroupAsync(user);
            Guid oneBill = await GroupRows.InsertGroupAsync(_app, user.UserId, kind: "OneBill");
            await GroupRows.InsertMemberAsync(_app, oneBill, user.UserId, GroupRows.OwnerMemberName, user.UserId);

            HttpResponseMessage response = await GroupRequests.ListAsync(user.Client);

            JsonElement groups = (await AuthRequests.ReadJsonAsync(response)).GetProperty("groups");
            Assert.Equal("Group", GroupRequests.RowWithId(groups, plainGroup).GetProperty("kind").GetString());
            Assert.Equal("OneBill", GroupRequests.RowWithId(groups, oneBill).GetProperty("kind").GetString());
        }

        private static async Task<JsonElement> ListedRowAsync(SignedInUser user, Guid groupId)
        {
            HttpResponseMessage response = await GroupRequests.ListAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            return GroupRequests.RowWithId(body.GetProperty("groups"), groupId);
        }
    }
}
