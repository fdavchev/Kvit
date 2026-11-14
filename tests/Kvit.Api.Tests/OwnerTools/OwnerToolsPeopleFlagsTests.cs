using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsPeopleFlagsTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        [Fact]
        public async Task Person_Normal_HasEveryFlagFalseAndNoGroups()
        {
            SignedInUser person = await _app.SignUpAsync();

            JsonElement item = await ItemOfAsync(person.UserId);

            Assert.False(item.GetProperty("isGuest").GetBoolean());
            Assert.False(item.GetProperty("isSuspended").GetBoolean());
            Assert.False(item.GetProperty("isAdmin").GetBoolean());
            Assert.False(item.GetProperty("deletionPending").GetBoolean());
            Assert.False(item.GetProperty("isDeleted").GetBoolean());
            Assert.Equal(0, item.GetProperty("groupCount").GetInt32());
            Assert.Equal(person.Form.Email, item.GetProperty("email").GetString());
        }

        [Fact]
        public async Task Person_Guest_IsGuestTrueWithNoEmail()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, setup.GroupId, "Baba Mara");

            JsonElement item = await ItemOfAsync(guest.UserId);

            Assert.True(item.GetProperty("isGuest").GetBoolean());
            Assert.Equal(JsonValueKind.Null, item.GetProperty("email").ValueKind);
            Assert.False(item.GetProperty("isSuspended").GetBoolean());
            Assert.Equal(1, item.GetProperty("groupCount").GetInt32());
        }

        [Fact]
        public async Task Person_Suspended_IsSuspendedTrueAndThenFalseAgainAfterUnsuspending()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();

            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            bool whileSuspended = (await ItemOfAsync(person.UserId)).GetProperty("isSuspended").GetBoolean();
            await OwnerToolsSteps.UnsuspendAsync(admin, person.UserId);
            bool afterwards = (await ItemOfAsync(person.UserId)).GetProperty("isSuspended").GetBoolean();

            Assert.True(whileSuspended);
            Assert.False(afterwards);
        }

        [Fact]
        public async Task Person_SuspendedBySql_IsSuspendedTrue()
        {
            SignedInUser person = await _app.SignUpSuspendedAsync();

            JsonElement item = await ItemOfAsync(person.UserId);

            Assert.True(item.GetProperty("isSuspended").GetBoolean());
        }

        [Fact]
        public async Task Person_Admin_IsAdminTrue()
        {
            SignedInUser admin = await _app.SharedAdminAsync();

            JsonElement item = await ItemOfAsync(admin.UserId);

            Assert.True(item.GetProperty("isAdmin").GetBoolean());
        }

        [Fact]
        public async Task Person_AdminWhoLostTheRole_IsAdminFalse()
        {
            SignedInUser former = await _app.SignInAdminAsync();
            await _app.RevokeAdminAsync(former.Form.Email);

            JsonElement item = await ItemOfAsync(former.UserId);

            Assert.False(item.GetProperty("isAdmin").GetBoolean());
        }

        [Fact]
        public async Task Person_WhoAskedToDeleteTheAccount_HasDeletionPendingTrueAndIsDeletedFalse()
        {
            SignedInUser person = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(person.Client, DeletionRequests.KeepName);

            JsonElement item = await ItemOfAsync(person.UserId);

            Assert.True(item.GetProperty("deletionPending").GetBoolean());
            Assert.False(item.GetProperty("isDeleted").GetBoolean());
        }

        [Fact]
        public async Task Person_WhoCancelledTheDeletionByLoggingIn_HasDeletionPendingFalse()
        {
            SignedInUser person = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(person.Client, DeletionRequests.KeepName);
            await _app.CreateLoggedInClientAsync(person.Form.Email, person.Form.Password);

            JsonElement item = await ItemOfAsync(person.UserId);

            Assert.False(item.GetProperty("deletionPending").GetBoolean());
            Assert.False(item.GetProperty("isDeleted").GetBoolean());
        }

        [Theory]
        [InlineData(DeletionRequests.KeepName, "Zorica Petrovska")]
        [InlineData(DeletionRequests.FormerMember, DeletionRequests.FormerMemberEnglish)]
        public async Task Person_Purged_IsListedAsDeletedWithoutEmailAndNotPending(string nameChoice, string expectedName)
        {
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            await DeletionSteps.RequestAndPurgeAsync(_app, person.Client, person.UserId, nameChoice);

            JsonElement item = await ItemOfAsync(person.UserId);

            Assert.True(item.GetProperty("isDeleted").GetBoolean());
            Assert.False(item.GetProperty("deletionPending").GetBoolean());
            Assert.Equal(JsonValueKind.Null, item.GetProperty("email").ValueKind);
            Assert.Equal(expectedName, item.GetProperty("displayName").GetString());
            Assert.False(item.GetProperty("isGuest").GetBoolean());
        }

        [Fact]
        public async Task Person_PurgedWithKeepName_IsFoundByTheirNameToo()
        {
            string token = OwnerToolsSteps.UniqueToken();
            SignedInUser person = await _app.SignUpAsync($"Kept {token}");
            await DeletionSteps.RequestAndPurgeAsync(_app, person.Client, person.UserId, DeletionRequests.KeepName);
            await OwnerToolsRows.SetCreatedAtAsync(_app, person.UserId, new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero));
            SignedInUser admin = await _app.SharedAdminAsync();

            List<Guid> ids = OwnerToolsRequests.IdsOf(await OwnerToolsRequests.ReadPeopleAsync(admin.Client, token));

            Assert.Equal([person.UserId], ids);
        }

        [Fact]
        public async Task GroupCount_PersonInTwoGroups_IsTwo()
        {
            SignedInUser person = await _app.SignUpAsync();
            await GroupRows.InsertGroupWithOwnerAsync(_app, person.UserId);
            await GroupRows.InsertGroupWithOwnerAsync(_app, person.UserId);

            JsonElement item = await ItemOfAsync(person.UserId);

            Assert.Equal(2, item.GetProperty("groupCount").GetInt32());
        }

        [Fact]
        public async Task GroupCount_PersonWhoLeftAGroup_DoesNotCountIt()
        {
            SignedInUser person = await _app.SignUpAsync();
            await GroupRows.InsertGroupWithOwnerAsync(_app, person.UserId);
            Guid left = await GroupRows.InsertGroupWithOwnerAsync(_app, person.UserId);
            await GroupRows.EndMembershipAsync(_app, left, person.UserId, "Left");

            JsonElement item = await ItemOfAsync(person.UserId);

            Assert.Equal(1, item.GetProperty("groupCount").GetInt32());
        }

        [Fact]
        public async Task GroupCount_PersonRemovedFromAGroup_DoesNotCountIt()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");

            JsonElement item = await ItemOfAsync(setup.Member.UserId);

            Assert.Equal(0, item.GetProperty("groupCount").GetInt32());
        }

        [Fact]
        public async Task GroupCount_GroupThatIsSoftDeleted_IsNotCounted()
        {
            SignedInUser person = await _app.SignUpAsync();
            await GroupRows.InsertGroupWithOwnerAsync(_app, person.UserId);
            Guid deleted = await GroupRows.InsertGroupWithOwnerAsync(_app, person.UserId);
            await GroupRows.SetDeletedAsync(_app, deleted, person.UserId, 1);

            JsonElement item = await ItemOfAsync(person.UserId);

            Assert.Equal(1, item.GetProperty("groupCount").GetInt32());
        }

        [Fact]
        public async Task GroupCount_GroupClosedByTheOwnerOfKvit_IsStillCounted()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(person);
            await OwnerToolsSteps.CloseGroupAsync(admin, groupId);

            JsonElement item = await ItemOfAsync(person.UserId);

            Assert.Equal(1, item.GetProperty("groupCount").GetInt32());
        }

        [Fact]
        public async Task GroupCount_TwoPeopleInTheSameGroup_EachCountOne()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement owner = await ItemOfAsync(setup.Owner.UserId);
            JsonElement member = await ItemOfAsync(setup.Member.UserId);

            Assert.Equal(1, owner.GetProperty("groupCount").GetInt32());
            Assert.Equal(1, member.GetProperty("groupCount").GetInt32());
        }

        private async Task<JsonElement> ItemOfAsync(Guid personId)
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            HttpResponseMessage response = await OwnerToolsRequests.PeopleAsync(admin.Client, personId.ToString());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement list = await AuthRequests.ReadJsonAsync(response);

            return Assert.Single(list.EnumerateArray());
        }
    }
}
