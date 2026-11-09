using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsClosedByAdminFieldTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        [Fact]
        public async Task CreateGroup_Answer_HasClosedByAdminFalse()
        {
            SignedInUser owner = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(owner.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.False(body.GetProperty("closedByAdmin").GetBoolean());
        }

        [Fact]
        public async Task GetGroup_NeverClosed_HasClosedByAdminFalseAndTheKnownFieldList()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.GetAsync(setup.Owner.Client, setup.GroupId);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.False(body.GetProperty("closedByAdmin").GetBoolean());
            Assert.Equal(GroupRequests.GroupFieldNames, GroupRequests.PropertyNamesOf(body));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetGroup_ClosedByTheOwnerOfKvit_HasClosedByAdminTrueForTheOwnerAndForMembers(bool callerIsOwner)
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);
            HttpClient caller = callerIsOwner ? setup.Owner.Client : setup.Member.Client;

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(caller, setup.GroupId));

            Assert.True(body.GetProperty("closedByAdmin").GetBoolean());
        }

        [Fact]
        public async Task GetGroup_ClosedByTheOwnerOfKvit_AGuestMemberSeesItToo()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Baba Mara");
            await OwnerToolsSteps.CloseGroupAsync(admin, group.GroupId);

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(guest.Client, group.GroupId));

            Assert.True(body.GetProperty("closedByAdmin").GetBoolean());
        }

        [Fact]
        public async Task GetGroup_ReopenedByTheOwnerOfKvit_HasClosedByAdminFalseAgain()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);
            await OwnerToolsSteps.ReopenGroupAsync(admin, setup.GroupId);

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(setup.Owner.Client, setup.GroupId));

            Assert.False(body.GetProperty("closedByAdmin").GetBoolean());
        }

        [Fact]
        public async Task GetGroup_ClosedByTheOwnerOfKvit_KeepsItsOwnStatusInTheAnswer()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(setup.Owner.Client, setup.GroupId));

            Assert.Equal("Open", body.GetProperty("status").GetString());
        }

        [Fact]
        public async Task GroupsList_GroupNeverClosed_RowHasClosedByAdminFalse()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement row = await RowOfAsync(setup.Owner, setup.GroupId);

            Assert.False(row.GetProperty("closedByAdmin").GetBoolean());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GroupsList_ClosedByTheOwnerOfKvit_RowHasClosedByAdminTrueForTheOwnerAndForMembers(bool callerIsOwner)
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);

            JsonElement row = await RowOfAsync(callerIsOwner ? setup.Owner : setup.Member, setup.GroupId);

            Assert.True(row.GetProperty("closedByAdmin").GetBoolean());
        }

        [Fact]
        public async Task GroupsList_ClosedByTheOwnerOfKvitAndAlsoFinished_RowHasClosedByAdminTrue()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await OwnerToolsSteps.CloseGroupAsync(admin, group.GroupId);

            JsonElement row = await RowOfAsync(group.Owner, group.GroupId);

            Assert.True(row.GetProperty("closedByAdmin").GetBoolean());
        }

        [Fact]
        public async Task GroupsList_ReopenedByTheOwnerOfKvit_RowHasClosedByAdminFalseAgain()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);
            await OwnerToolsSteps.ReopenGroupAsync(admin, setup.GroupId);

            JsonElement row = await RowOfAsync(setup.Owner, setup.GroupId);

            Assert.False(row.GetProperty("closedByAdmin").GetBoolean());
        }

        [Fact]
        public async Task GroupsList_OneClosedGroupAndOneNot_OnlyTheClosedRowIsMarked()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser owner = await _app.SignUpAsync();
            Guid closedId = await _app.CreateGroupAsync(owner);
            Guid openId = await _app.CreateGroupAsync(owner);
            await OwnerToolsSteps.CloseGroupAsync(admin, closedId);

            JsonElement closedRow = await RowOfAsync(owner, closedId);
            JsonElement openRow = await RowOfAsync(owner, openId);

            Assert.True(closedRow.GetProperty("closedByAdmin").GetBoolean());
            Assert.False(openRow.GetProperty("closedByAdmin").GetBoolean());
        }

        private async Task<JsonElement> RowOfAsync(SignedInUser viewer, Guid groupId)
        {
            HttpResponseMessage response = await GroupRequests.ListAsync(viewer.Client);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            IEnumerable<JsonElement> rows = body.GetProperty("groups").EnumerateArray().Concat(body.GetProperty("finishedGroups").EnumerateArray());

            return rows.Single(row => row.GetProperty("id").GetGuid() == groupId);
        }
    }
}
