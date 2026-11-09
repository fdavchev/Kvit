using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsLogTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        [Fact]
        public async Task Log_AfterOneSuspend_ItemHasExactlyTheSevenDocumentedFields()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);

            JsonElement item = Assert.Single(await ItemsOfAsync(admin, adminName));

            Assert.Equal(OwnerToolsRequests.LogFieldNames, GroupRequests.PropertyNamesOf(item));
        }

        [Fact]
        public async Task Log_AfterOneSuspend_ItemShowsTheActionThePersonTheReasonTheAdminAndTheTime()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Dragan Ilievski");
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId, "  Reported twice  ");

            JsonElement item = Assert.Single(await ItemsOfAsync(admin, adminName));

            Assert.Equal("Suspended", item.GetProperty("action").GetString());
            Assert.Equal("person", item.GetProperty("targetKind").GetString());
            Assert.Equal("Dragan Ilievski", item.GetProperty("targetLabel").GetString());
            Assert.Equal("Reported twice", item.GetProperty("reason").GetString());
            Assert.Equal(adminName, item.GetProperty("adminName").GetString());
            Assert.Equal(_app.Clock.GetUtcNow(), DateTimeOffset.Parse(item.GetProperty("createdAt").GetString()!, CultureInfo.InvariantCulture));
            Assert.True(item.GetProperty("id").GetInt64() > 0);
        }

        [Fact]
        public async Task Log_AfterClosingAGroup_ItemHasTargetKindGroupAndTheGroupName()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId, "Hateful content");

            JsonElement item = Assert.Single(await ItemsOfAsync(admin, adminName));

            Assert.Equal("GroupClosed", item.GetProperty("action").GetString());
            Assert.Equal("group", item.GetProperty("targetKind").GetString());
            Assert.Equal(GroupRows.DefaultName, item.GetProperty("targetLabel").GetString());
            Assert.Equal("Hateful content", item.GetProperty("reason").GetString());
        }

        [Fact]
        public async Task Log_AfterAllFiveKindsOfAction_ListsThemNewestFirstWithTheirNames()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            SignedInUser toErase = await _app.SignUpAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.UnsuspendAsync(admin, person.UserId);
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.EraseNameAsync(admin, toErase.UserId);
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.ReopenGroupAsync(admin, setup.GroupId);

            List<JsonElement> items = await ItemsOfAsync(admin, adminName);

            Assert.Equal(
                ["GroupReopened", "GroupClosed", "NameErased", "Unsuspended", "Suspended"],
                items.Select(item => item.GetProperty("action").GetString()));
            Assert.Equal(
                ["group", "group", "person", "person", "person"],
                items.Select(item => item.GetProperty("targetKind").GetString()));
        }

        [Fact]
        public async Task Log_Ids_AreDecreasingDownTheList()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.UnsuspendAsync(admin, person.UserId);

            List<long> ids = [.. (await ItemsOfAsync(admin, adminName)).Select(item => item.GetProperty("id").GetInt64())];

            Assert.Equal(2, ids.Count);
            Assert.True(ids[0] > ids[1], $"The ids are {string.Join(", ", ids)}.");
        }

        [Fact]
        public async Task Log_PersonRenamedAfterwards_OldRowKeepsTheOldNameAndTheNewRowGetsTheNewOne()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Dragan Ilievski");
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            await UserRows.SetDisplayNameAsync(_app, person.UserId, "Dragan I.");
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.UnsuspendAsync(admin, person.UserId);

            List<JsonElement> items = await ItemsOfAsync(admin, adminName);

            Assert.Equal(["Dragan I.", "Dragan Ilievski"], items.Select(item => item.GetProperty("targetLabel").GetString()));
        }

        [Fact]
        public async Task Log_GroupRenamedAfterwards_OldRowKeepsTheOldNameAndTheNewRowGetsTheNewOne()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await OwnerToolsSteps.CloseGroupAsync(admin, setup.GroupId);
            await OwnerToolsRows.SetGroupNameAsync(_app, setup.GroupId, "Renamed trip");
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.ReopenGroupAsync(admin, setup.GroupId);

            List<JsonElement> items = await ItemsOfAsync(admin, adminName);

            Assert.Equal(["Renamed trip", GroupRows.DefaultName], items.Select(item => item.GetProperty("targetLabel").GetString()));
        }

        [Fact]
        public async Task Log_ErasedPerson_RowKeepsTheNameFromBeforeTheErase()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);

            JsonElement item = Assert.Single(await ItemsOfAsync(admin, adminName));

            Assert.Equal("Zorica Petrovska", item.GetProperty("targetLabel").GetString());
        }

        [Fact]
        public async Task Log_AdminRenamedAfterwards_ShowsTheCurrentNameOnOldRows()
        {
            (SignedInUser admin, _) = await NewAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            string newName = $"Renamed {OwnerToolsSteps.UniqueToken()}";
            await UserRows.SetDisplayNameAsync(_app, admin.UserId, newName);

            List<JsonElement> items = await ItemsOfAsync(admin, newName);

            Assert.Equal(newName, Assert.Single(items).GetProperty("adminName").GetString());
        }

        [Fact]
        public async Task Log_ActionsOfAnotherAdmin_AreListedToo()
        {
            (SignedInUser first, string firstName) = await NewAdminAsync();
            (SignedInUser second, string secondName) = await NewAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            await OwnerToolsSteps.SuspendAsync(first, person.UserId);
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.UnsuspendAsync(second, person.UserId);

            JsonElement logSeenBySecond = await OwnerToolsRequests.ReadLogAsync(second.Client, OwnerToolsRequests.LogMaxLimit);

            Assert.Single(AboutAdmin(logSeenBySecond, firstName));
            Assert.Single(AboutAdmin(logSeenBySecond, secondName));
        }

        [Fact]
        public async Task Log_RepeatedSuspend_ListsOnlyTheFirst()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);

            Assert.Single(await ItemsOfAsync(admin, adminName));
        }

        [Fact]
        public async Task Log_FailedAndRefusedCalls_AddNoRow()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            SignedInUser otherAdmin = await _app.SignInAdminAsync();
            SignedInUser normal = await _app.SignUpAsync();

            await OwnerToolsRequests.SuspendAsync(admin.Client, person.UserId, string.Empty);
            await OwnerToolsRequests.SuspendAsync(admin.Client, Guid.NewGuid());
            await OwnerToolsRequests.SuspendAsync(admin.Client, otherAdmin.UserId);
            await OwnerToolsRequests.SuspendAsync(admin.Client, admin.UserId);
            await OwnerToolsRequests.EraseNameAsync(admin.Client, otherAdmin.UserId);
            await OwnerToolsRequests.CloseAsync(admin.Client, Guid.NewGuid());
            await OwnerToolsRequests.SuspendAsync(normal.Client, person.UserId);
            await OwnerToolsRequests.SuspendAsync(_app.CreateClient(), person.UserId);

            Assert.Empty(await ItemsOfAsync(admin, adminName));
            Assert.Empty(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId));
        }

        [Fact]
        public async Task Log_ReadingPeopleGroupsAndTheLog_AddsNoRow()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            int before = await OwnerToolsRows.CountActionsAsync(_app);

            await OwnerToolsRequests.PeopleAsync(admin.Client, "ab");
            await OwnerToolsRequests.GroupAsync(admin.Client, setup.GroupId);
            await OwnerToolsRequests.LogAsync(admin.Client);

            Assert.Equal(before, await OwnerToolsRows.CountActionsAsync(_app));
            Assert.Empty(await ItemsOfAsync(admin, adminName));
        }

        [Fact]
        public async Task Log_ActionAboutAGroupThatWasHardDeletedLater_IsStillListed()
        {
            (SignedInUser admin, string adminName) = await NewAdminAsync();
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupAsync(_app, owner.UserId, name: "Doomed trip");
            await OwnerToolsSteps.CloseGroupAsync(admin, groupId);
            await _app.ExecuteAsync("DELETE FROM groups WHERE id = @id", new NpgsqlParameter("id", groupId));

            JsonElement item = Assert.Single(await ItemsOfAsync(admin, adminName));

            Assert.Equal("Doomed trip", item.GetProperty("targetLabel").GetString());
            Assert.Equal("group", item.GetProperty("targetKind").GetString());
        }

        [Fact]
        public async Task Log_Answer_IsJsonWithAnArrayAtTheTop()
        {
            SignedInUser admin = await _app.SharedAdminAsync();

            HttpResponseMessage response = await OwnerToolsRequests.LogAsync(admin.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(JsonValueKind.Array, (await OwnerToolsRequests.ReadLogAsync(admin.Client)).ValueKind);
        }

        private async Task<(SignedInUser Admin, string AdminName)> NewAdminAsync()
        {
            string adminName = $"Admin {OwnerToolsSteps.UniqueToken()}";
            SignedInUser admin = await _app.SignInAdminAsync(adminName);

            return (admin, adminName);
        }

        private static List<JsonElement> AboutAdmin(JsonElement log, string adminName)
        {
            return [.. log.EnumerateArray().Where(item => item.GetProperty("adminName").GetString() == adminName)];
        }

        private static async Task<List<JsonElement>> ItemsOfAsync(SignedInUser admin, string adminName)
        {
            return AboutAdmin(await OwnerToolsRequests.ReadLogAsync(admin.Client, OwnerToolsRequests.LogMaxLimit), adminName);
        }
    }
}
