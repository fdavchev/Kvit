using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class UpdateGroupTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        private const string NewEmoji = "\U0001F37D";

        [Fact]
        public async Task Update_Owner_Answers204AndChangesTheRow()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(owner.Client, groupId, "Rome", NewEmoji, "EUR");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Rome", await GroupRows.GroupValueAsync(_app, groupId, "name"));
            Assert.Equal(NewEmoji, await GroupRows.GroupValueAsync(_app, groupId, "emoji"));
            Assert.Equal("EUR", await GroupRows.GroupValueAsync(_app, groupId, "default_currency"));
        }

        [Fact]
        public async Task Update_Owner_ShowsTheChangeInGet()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRequests.UpdateAsync(owner.Client, groupId, "Rome", NewEmoji, "EUR");

            HttpResponseMessage response = await GroupRequests.GetAsync(owner.Client, groupId);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Rome", body.GetProperty("name").GetString());
            Assert.Equal(NewEmoji, body.GetProperty("emoji").GetString());
            Assert.Equal("EUR", body.GetProperty("defaultCurrency").GetString());
        }

        [Fact]
        public async Task Update_NameWithSurroundingSpaces_StoresTheTrimmedName()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            await GroupRequests.UpdateAsync(owner.Client, groupId, "  Rome  ");

            Assert.Equal("Rome", await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task Update_MemberWhoIsNotTheOwner_Answers403GroupNotOwnerAndChangesNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(member.Client, groupId, "Rome", NewEmoji, "EUR");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(GroupRows.DefaultName, await GroupRows.GroupValueAsync(_app, groupId, "name"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "GroupRenamed"));
        }

        [Fact]
        public async Task Update_NonMember_Answers404GroupNotFound()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser stranger = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(stranger.Client, groupId, "Rome");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(GroupRows.DefaultName, await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task Update_UnknownGroupId_Answers404GroupNotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.UpdateAsync(user.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Update_DeletedGroup_Answers404GroupNotFound()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.SetDeletedAsync(_app, groupId, owner.UserId, 0);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(owner.Client, groupId, "Rome");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Update_MemberWhoWasRemoved_Answers404GroupNotFound()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId);
            await GroupRows.EndMembershipAsync(_app, groupId, member.UserId, "Removed");

            HttpResponseMessage response = await GroupRequests.UpdateAsync(member.Client, groupId, "Rome");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Update_NonMemberWithInvalidInput_Answers404BeforeValidating()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser stranger = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(stranger.Client, groupId, "");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Update_NonOwnerMemberWithInvalidInput_Answers403BeforeValidating()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, "Ana", owner.UserId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(member.Client, groupId, "");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Theory]
        [MemberData(nameof(InvalidGroupInput.Cases), MemberType = typeof(InvalidGroupInput))]
        public async Task Update_InvalidInput_Answers400WithTheErrorCodeAndChangesNothing(string name, string emoji, string currency, string expectedErrorCode)
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(owner.Client, groupId, name, emoji, currency);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(GroupRows.DefaultName, await GroupRows.GroupValueAsync(_app, groupId, "name"));
            Assert.Equal(GroupRows.DefaultEmoji, await GroupRows.GroupValueAsync(_app, groupId, "emoji"));
            Assert.Equal(GroupRows.DefaultCurrency, await GroupRows.GroupValueAsync(_app, groupId, "default_currency"));
            Assert.Equal(0, await GroupRows.CountEventsAsync(_app, groupId));
        }

        [Fact]
        public async Task Update_Name_WritesOneGroupRenamedEventWithTheOldAndNewName()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            await GroupRequests.UpdateAsync(owner.Client, groupId, "Rome");

            Assert.Equal(1, await GroupRows.CountEventsAsync(_app, groupId));
            string? changesJson = await GroupRows.EventValueAsync(_app, groupId, "GroupRenamed", "changes");
            Assert.Equal([("name", GroupRows.DefaultName, "Rome")], ActivityChanges.Parse(changesJson));
        }

        [Fact]
        public async Task Update_Name_RecordsTheCallerAsTheActorOfTheEvent()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            await GroupRequests.UpdateAsync(owner.Client, groupId, "Rome");

            Assert.Equal(owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, groupId, "GroupRenamed", "actor_user_id"));
        }

        [Fact]
        public async Task Update_Emoji_WritesOneGroupSettingsChangedEventWithTheOldAndNewEmoji()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            await GroupRequests.UpdateAsync(owner.Client, groupId, emoji: NewEmoji);

            Assert.Equal(1, await GroupRows.CountEventsAsync(_app, groupId));
            string? changesJson = await GroupRows.EventValueAsync(_app, groupId, "GroupSettingsChanged", "changes");
            Assert.Equal([("emoji", GroupRows.DefaultEmoji, NewEmoji)], ActivityChanges.Parse(changesJson));
        }

        [Fact]
        public async Task Update_Currency_WritesOneGroupSettingsChangedEventWithTheOldAndNewCurrency()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            await GroupRequests.UpdateAsync(owner.Client, groupId, currency: "EUR");

            Assert.Equal(1, await GroupRows.CountEventsAsync(_app, groupId));
            string? changesJson = await GroupRows.EventValueAsync(_app, groupId, "GroupSettingsChanged", "changes");
            Assert.Equal([("defaultCurrency", "MKD", "EUR")], ActivityChanges.Parse(changesJson));
        }

        [Fact]
        public async Task Update_EmojiAndCurrency_WritesExactlyOneGroupSettingsChangedEventListingBoth()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            await GroupRequests.UpdateAsync(owner.Client, groupId, emoji: NewEmoji, currency: "EUR");

            Assert.Equal(1, await GroupRows.CountEventsAsync(_app, groupId));
            string? changesJson = await GroupRows.EventValueAsync(_app, groupId, "GroupSettingsChanged", "changes");
            List<(string Field, string Old, string New)> changes = [.. ActivityChanges.Parse(changesJson).OrderBy(change => change.Field, StringComparer.Ordinal)];
            Assert.Equal([("defaultCurrency", "MKD", "EUR"), ("emoji", GroupRows.DefaultEmoji, NewEmoji)], changes);
        }

        [Fact]
        public async Task Update_NameAndEmoji_WritesBothAGroupRenamedAndAGroupSettingsChangedEvent()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            await GroupRequests.UpdateAsync(owner.Client, groupId, "Rome", NewEmoji);

            Assert.Equal(2, await GroupRows.CountEventsAsync(_app, groupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "GroupRenamed"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "GroupSettingsChanged"));
        }

        [Fact]
        public async Task Update_NothingChanged_Answers204AndWritesNoEvent()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(owner.Client, groupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, await GroupRows.CountEventsAsync(_app, groupId));
        }

        [Fact]
        public async Task Update_NameThatDiffersOnlyBySurroundingSpaces_WritesNoEvent()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, owner.UserId);

            HttpResponseMessage response = await GroupRequests.UpdateAsync(owner.Client, groupId, $"  {GroupRows.DefaultName}  ");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, await GroupRows.CountEventsAsync(_app, groupId));
        }

        [Fact]
        public async Task Update_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await GroupRequests.UpdateAsync(client, Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
