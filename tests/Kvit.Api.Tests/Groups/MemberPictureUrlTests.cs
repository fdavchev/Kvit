using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class MemberPictureUrlTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        private const string MemberPictureUrl = "https://lh3.googleusercontent.com/a/bojan-picture=s96-c";
        private const string OwnerPictureUrl = "https://lh3.googleusercontent.com/a/ana-picture=s96-c";
        private const string ChangedPictureUrl = "https://lh3.googleusercontent.com/a/changed-picture=s96-c";

        [Fact]
        public async Task List_AccountMemberWithAPicture_ShowsThePictureUrlToTheOwner()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Member.UserId, MemberPictureUrl);

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal(MemberPictureUrl, row.GetProperty("pictureUrl").GetString());
        }

        [Fact]
        public async Task List_OwnerWithAPicture_ShowsThePictureUrlToAnotherMember()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Owner.UserId, OwnerPictureUrl);

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Member.Client, setup.GroupId, setup.OwnerRowId);

            Assert.Equal(OwnerPictureUrl, row.GetProperty("pictureUrl").GetString());
        }

        [Fact]
        public async Task List_CallerWithAPicture_ShowsThePictureUrlOnTheCallersOwnRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Member.UserId, MemberPictureUrl);

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Member.Client, setup.GroupId, setup.MemberRowId);

            Assert.True(row.GetProperty("isYou").GetBoolean());
            Assert.Equal(MemberPictureUrl, row.GetProperty("pictureUrl").GetString());
        }

        [Fact]
        public async Task List_AccountMemberWithoutAPicture_ShowsANullPictureUrl()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal(JsonValueKind.Null, row.GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task List_PlainName_ShowsANullPictureUrl()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Owner.UserId, OwnerPictureUrl);
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Owner.Client, setup.GroupId, plainId);

            Assert.Equal(JsonValueKind.Null, row.GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task List_EachRowCarriesThePictureOfItsOwnAccount()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Owner.UserId, OwnerPictureUrl);
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Member.UserId, MemberPictureUrl);
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);

            JsonElement members = (await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId)).GetProperty("members");

            Assert.Equal(OwnerPictureUrl, GroupRequests.RowWithId(members, setup.OwnerRowId).GetProperty("pictureUrl").GetString());
            Assert.Equal(MemberPictureUrl, GroupRequests.RowWithId(members, setup.MemberRowId).GetProperty("pictureUrl").GetString());
            Assert.Equal(JsonValueKind.Null, GroupRequests.RowWithId(members, plainId).GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task List_ClaimedNameRow_ShowsThePictureOfTheAccountThatTookTheName()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Member.UserId, MemberPictureUrl);
            Guid claimedId = await _app.SetUpClaimedNameAsync(setup, "Marko");

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Member.Client, setup.GroupId, claimedId);

            Assert.Equal("Marko", row.GetProperty("name").GetString());
            Assert.Equal(MemberPictureUrl, row.GetProperty("pictureUrl").GetString());
        }

        [Fact]
        public async Task List_PictureChangedAfterwards_ShowsTheNewUrlAndThenNullWhenCleared()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Member.UserId, MemberPictureUrl);
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Member.UserId, ChangedPictureUrl);

            JsonElement changed = await GroupRequests.MemberRowAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Member.UserId, null);
            JsonElement cleared = await GroupRequests.MemberRowAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal(ChangedPictureUrl, changed.GetProperty("pictureUrl").GetString());
            Assert.Equal(JsonValueKind.Null, cleared.GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task List_RemovedAccountMemberWithAPicture_ShowsThePictureUrlInTheRemovedList()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Member.UserId, MemberPictureUrl);
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");

            JsonElement removed = (await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId)).GetProperty("removed");

            Assert.Equal(MemberPictureUrl, GroupRequests.RowWithId(removed, setup.MemberRowId).GetProperty("pictureUrl").GetString());
        }

        [Fact]
        public async Task List_RemovedAccountMemberWithoutAPicture_ShowsANullPictureUrlInTheRemovedList()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");

            JsonElement removed = (await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId)).GetProperty("removed");

            Assert.Equal(JsonValueKind.Null, GroupRequests.RowWithId(removed, setup.MemberRowId).GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task List_RemovedPlainName_ShowsANullPictureUrlInTheRemovedList()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid removedPlainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Zora", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");

            JsonElement removed = (await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId)).GetProperty("removed");

            Assert.Equal(JsonValueKind.Null, GroupRequests.RowWithId(removed, removedPlainId).GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task List_RemovedMemberRow_HasExactlyTheIdDisplayNameAndPictureUrl()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Member.UserId, MemberPictureUrl);
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");

            JsonElement removed = (await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId)).GetProperty("removed");

            Assert.Equal(["displayName", "id", "pictureUrl"], GroupRequests.PropertyNamesOf(Assert.Single(removed.EnumerateArray())));
        }

        [Fact]
        public async Task Add_PlainName_AnswersTheNewRowWithANullPictureUrl()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.AddMemberAsync(setup.Owner.Client, setup.GroupId, "Marko");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task List_PersonWhoIsNotInTheGroup_DoesNotGetAnyPictureUrl()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await UserRows.SetGooglePictureUrlAsync(_app, setup.Member.UserId, MemberPictureUrl);
            SignedInUser stranger = await _app.SignUpAsync("Stranger");

            HttpResponseMessage response = await GroupRequests.ListMembersAsync(stranger.Client, setup.GroupId);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain(MemberPictureUrl, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
    }
}
