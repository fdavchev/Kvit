using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Microsoft.Net.Http.Headers;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class JoinAsGuestTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        private const string FreeName = "Marko";
        private const string NewName = "Cvetan";
        private const int LoginDays = 90;

        [Fact]
        public async Task JoinAsGuest_TappingAFreeName_Answers200WithExactlyTheGroupId()
        {
            GuestScene scene = await CreateSceneAsync();

            HttpResponseMessage response = await GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: scene.FreeNameRowId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["groupId"], GroupRequests.PropertyNamesOf(body));
            Assert.Equal(scene.Setup.GroupId, body.GetProperty("groupId").GetGuid());
        }

        [Fact]
        public async Task JoinAsGuest_TappingAFreeName_PutsTheGuestOnTheNameRowAndAddsNoRow()
        {
            GuestScene scene = await CreateSceneAsync();
            int rowsBefore = await GroupRows.CountMembersAsync(_app, scene.Setup.GroupId);

            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: scene.FreeNameRowId));

            Guid guestId = await GuestIdAsync(scene.Guest);
            Assert.Equal(rowsBefore, await GroupRows.CountMembersAsync(_app, scene.Setup.GroupId));
            Assert.Equal(guestId.ToString(), await GroupRows.MemberRowValueAsync(_app, scene.FreeNameRowId, "user_id"));
            Assert.Equal("true", await GroupRows.MemberRowValueAsync(_app, scene.FreeNameRowId, "claimed_at > now() - interval '1 minute'"));
            Assert.Equal(FreeName, await GroupRows.MemberRowValueAsync(_app, scene.FreeNameRowId, "name"));
            Assert.Equal(1, await GroupRows.CountMemberRowsOfUserAsync(_app, scene.Setup.GroupId, guestId));
        }

        [Fact]
        public async Task JoinAsGuest_TappingAFreeName_NamesTheGuestAfterThatName()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: scene.FreeNameRowId));

            JsonElement me = await MeOfAsync(scene.Guest);
            Assert.Equal(FreeName, me.GetProperty("displayName").GetString());
        }

        [Fact]
        public async Task JoinAsGuest_TappingAFreeName_WritesMemberJoinedAndMemberClaimedByTheGuestAboutTheNameRow()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: scene.FreeNameRowId));

            Guid guestId = await GuestIdAsync(scene.Guest);
            Guid groupId = scene.Setup.GroupId;
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "MemberJoined"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "MemberClaimed"));
            Assert.Equal(guestId.ToString(), await GroupRows.EventValueAsync(_app, groupId, "MemberJoined", "actor_user_id"));
            Assert.Equal(guestId.ToString(), await GroupRows.EventValueAsync(_app, groupId, "MemberClaimed", "actor_user_id"));
            Assert.Equal(scene.FreeNameRowId.ToString(), await GroupRows.EventValueAsync(_app, groupId, "MemberJoined", "member_id"));
            Assert.Equal(scene.FreeNameRowId.ToString(), await GroupRows.EventValueAsync(_app, groupId, "MemberClaimed", "member_id"));
        }

        [Fact]
        public async Task JoinAsGuest_WithANewName_AddsOneRowForTheGuestUnderTheTrimmedName()
        {
            GuestScene scene = await CreateSceneAsync();
            int rowsBefore = await GroupRows.CountMembersAsync(_app, scene.Setup.GroupId);

            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: $"  {NewName}  "));

            Guid guestId = await GuestIdAsync(scene.Guest);
            Assert.Equal(rowsBefore + 1, await GroupRows.CountMembersAsync(_app, scene.Setup.GroupId));
            Guid rowId = await GroupRows.CurrentMemberIdAsync(_app, scene.Setup.GroupId, guestId);
            Assert.Equal(NewName, await GroupRows.MemberRowValueAsync(_app, rowId, "name"));
            Assert.Equal(guestId.ToString(), await GroupRows.MemberRowValueAsync(_app, rowId, "added_by_user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, rowId, "claimed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, scene.FreeNameRowId, "user_id"));
            Assert.Equal(NewName, (await MeOfAsync(scene.Guest)).GetProperty("displayName").GetString());
        }

        [Fact]
        public async Task JoinAsGuest_WithANewName_WritesOneMemberJoinedByTheGuestAndNoClaimEvent()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: NewName));

            Guid guestId = await GuestIdAsync(scene.Guest);
            Guid rowId = await GroupRows.CurrentMemberIdAsync(_app, scene.Setup.GroupId, guestId);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, scene.Setup.GroupId, "MemberJoined"));
            Assert.Equal(guestId.ToString(), await GroupRows.EventValueAsync(_app, scene.Setup.GroupId, "MemberJoined", "actor_user_id"));
            Assert.Equal(rowId.ToString(), await GroupRows.EventValueAsync(_app, scene.Setup.GroupId, "MemberJoined", "member_id"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, scene.Setup.GroupId, "MemberClaimed"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task JoinAsGuest_RecordsExactlyASignedUpAsGuestAndAJoinedViaInviteUsageEvent(bool tapsAFreeName)
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertOkAsync(JoinAsync(scene, tapsAFreeName));

            Guid guestId = await GuestIdAsync(scene.Guest);
            List<UsageEvent> usageEvents = await _app.UsageEventsOfAsync(guestId);
            Assert.Equal(2, usageEvents.Count);
            UsageEvent signedUp = Assert.Single(usageEvents, usageEvent => usageEvent.Type == UsageEventType.SignedUp);
            Assert.Equal("Guest", signedUp.Detail);
            Assert.Single(usageEvents, usageEvent => usageEvent.Type == UsageEventType.JoinedViaInvite);
        }

        [Fact]
        public async Task JoinAsGuest_StoresAGuestUserWithNoEmailNoPasswordAndTheSentLanguageAndTimeZone()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: NewName, language: "mk", timeZone: "America/New_York"));

            Guid guestId = await GuestIdAsync(scene.Guest);
            List<string?> stored = await _app.QueryAsync(
                "SELECT concat_ws('|', is_guest::text, (email IS NULL)::text, (normalized_email IS NULL)::text, (password_hash IS NULL)::text, (user_name IS NOT NULL)::text, language, time_zone, must_change_password::text) FROM users WHERE id = @id",
                new NpgsqlParameter("id", guestId));
            Assert.Equal("true|true|true|true|true|mk|America/New_York|false", Assert.Single(stored));
        }

        [Fact]
        public async Task JoinAsGuest_TwoGuests_GetTwoDifferentUsersWithDifferentUserNames()
        {
            GuestScene scene = await CreateSceneAsync();
            HttpClient secondGuest = _app.CreateClient();

            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: NewName));
            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(secondGuest, scene.Setup.InviteToken, name: "Dana"));

            Guid firstId = await GuestIdAsync(scene.Guest);
            Guid secondId = await GuestIdAsync(secondGuest);
            Assert.NotEqual(firstId, secondId);
            List<string?> userNames = await _app.QueryAsync(
                "SELECT user_name FROM users WHERE id = @first OR id = @second",
                new NpgsqlParameter("first", firstId),
                new NpgsqlParameter("second", secondId));
            Assert.Equal(2, userNames.Distinct().Count());
        }

        [Fact]
        public async Task JoinAsGuest_SetsTheHttpOnlyLoginCookieForAbout90Days()
        {
            GuestScene scene = await CreateSceneAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            HttpResponseMessage response = await GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: NewName);

            SetCookieHeaderValue? cookie = AuthCookie.Find(response);
            Assert.NotNull(cookie);
            Assert.True(cookie.HttpOnly);
            DateTimeOffset expiresAt = AuthCookie.ExpiresAt(cookie, now) ?? throw new InvalidOperationException("The kvit_auth cookie of a guest has neither Expires nor Max-Age.");
            Assert.InRange(expiresAt - now, TimeSpan.FromDays(LoginDays - 1), TimeSpan.FromDays(LoginDays + 1));
        }

        [Fact]
        public async Task JoinAsGuest_ThenMe_AnswersAGuestWithNoEmailAndNoPassword()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: NewName));

            JsonElement me = await MeOfAsync(scene.Guest);
            Assert.True(me.GetProperty("isGuest").GetBoolean());
            Assert.Equal(JsonValueKind.Null, me.GetProperty("email").ValueKind);
            Assert.False(me.GetProperty("hasPassword").GetBoolean());
            Assert.False(me.GetProperty("mustChangePassword").GetBoolean());
            Assert.Equal(JsonValueKind.Null, me.GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task JoinAsGuest_ThenTheGroup_OpensForTheGuestWithTheNewMemberCounted()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: NewName));

            Assert.Equal(4, await GroupRequests.MemberCountOfAsync(scene.Guest, scene.Setup.GroupId));
            JsonElement list = await AuthRequests.ReadJsonAsync(await GroupRequests.ListAsync(scene.Guest));
            Assert.Contains(scene.Setup.GroupId, GroupRequests.IdsOf(list.GetProperty("groups")));
        }

        [Fact]
        public async Task JoinAsGuest_WithoutKeepingTheCookie_LeavesEveryNextRequestSignedOut()
        {
            GuestScene scene = await CreateSceneAsync();
            HttpClient withoutCookies = _app.CreateClientWithoutCookies();

            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(withoutCookies, scene.Setup.InviteToken, name: NewName));

            Assert.Equal(HttpStatusCode.Unauthorized, (await AuthRequests.GetMeAsync(withoutCookies)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await GroupRequests.GetAsync(withoutCookies, scene.Setup.GroupId)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await GroupRequests.JoinAsync(withoutCookies, scene.Setup.InviteToken)).StatusCode);
        }

        [Theory]
        [InlineData(GroupsApp.MemberName)]
        [InlineData("bojan")]
        [InlineData(GroupsApp.OwnerName)]
        [InlineData("marko")]
        public async Task JoinAsGuest_NewNameEqualToTheNameOfSomeoneInTheGroup_Answers400MemberNameTakenAndCreatesNoUser(string name)
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertRefusedWithoutAGuestAsync(scene, () => GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: name), HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
        }

        [Fact]
        public async Task JoinAsGuest_BothAFreeNameAndANewName_Answers400GuestChoiceInvalidAndCreatesNoUser()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertRefusedWithoutAGuestAsync(scene, () => GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: scene.FreeNameRowId, name: NewName), HttpStatusCode.BadRequest, ResultCodes.INVITE_GUEST_CHOICE_INVALID);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, scene.FreeNameRowId, "user_id"));
        }

        [Fact]
        public async Task JoinAsGuest_NeitherAFreeNameNorANewName_Answers400GuestChoiceInvalidAndCreatesNoUser()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertRefusedWithoutAGuestAsync(scene, () => GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken), HttpStatusCode.BadRequest, ResultCodes.INVITE_GUEST_CHOICE_INVALID);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task JoinAsGuest_BlankNewName_Answers400DisplayNameInvalidAndCreatesNoUser(string name)
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertRefusedWithoutAGuestAsync(scene, () => GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: name), HttpStatusCode.BadRequest, ResultCodes.AUTH_DISPLAY_NAME_INVALID);
        }

        [Fact]
        public async Task JoinAsGuest_UnsupportedLanguage_Answers400LanguageInvalidAndCreatesNoUser()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertRefusedWithoutAGuestAsync(scene, () => GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: NewName, language: "de"), HttpStatusCode.BadRequest, ResultCodes.LANGUAGE_INVALID);
        }

        [Fact]
        public async Task JoinAsGuest_UnknownTimeZone_Answers400TimeZoneInvalidAndCreatesNoUser()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertRefusedWithoutAGuestAsync(scene, () => GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: scene.FreeNameRowId, timeZone: "Mars/Olympus"), HttpStatusCode.BadRequest, ResultCodes.TIME_ZONE_INVALID);
        }

        [Fact]
        public async Task JoinAsGuest_UnknownToken_Answers404InviteNotFoundAndCreatesNoUser()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertRefusedWithoutAGuestAsync(scene, () => GroupRequests.JoinAsGuestAsync(scene.Guest, "no-such-token", name: NewName), HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }

        [Fact]
        public async Task JoinAsGuest_TokenOfADeletedGroup_Answers404InviteNotFoundAndCreatesNoUser()
        {
            GuestScene scene = await CreateSceneAsync();
            await GroupRows.SetDeletedAsync(_app, scene.Setup.GroupId, scene.Setup.Owner.UserId, 0);

            await AssertRefusedWithoutAGuestAsync(scene, () => GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: scene.FreeNameRowId), HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }

        [Fact]
        public async Task JoinAsGuest_TappingTheRowOfAPersonWithAnAccount_Answers404MemberNotFoundAndCreatesNoUser()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertRefusedWithoutAGuestAsync(scene, () => GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: scene.Setup.MemberRowId), HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task JoinAsGuest_TappingAnUnknownRow_Answers404MemberNotFoundAndCreatesNoUser()
        {
            GuestScene scene = await CreateSceneAsync();

            await AssertRefusedWithoutAGuestAsync(scene, () => GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: Guid.NewGuid()), HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Theory]
        [InlineData("Finished", true, ResultCodes.GROUP_FINISHED)]
        [InlineData("Finished", false, ResultCodes.GROUP_FINISHED)]
        [InlineData("Closing", true, ResultCodes.GROUP_CLOSING)]
        [InlineData("Closing", false, ResultCodes.GROUP_CLOSING)]
        public async Task JoinAsGuest_IntoAFinishedOrClosingGroup_Answers400AndRollsTheGuestBackSoNoUserRemains(string status, bool tapsAFreeName, string expectedCode)
        {
            GuestScene scene = await CreateSceneAsync();
            await SetStatusAsync(scene.Setup.GroupId, status);
            string before = await GroupRows.SnapshotAsync(_app, scene.Setup.GroupId);

            await AssertRefusedWithoutAGuestAsync(scene, () => JoinAsync(scene, tapsAFreeName), HttpStatusCode.BadRequest, expectedCode);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, scene.Setup.GroupId));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, scene.FreeNameRowId, "user_id"));
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"token\":null,\"name\":\"Cvetan\",\"language\":\"en\",\"timeZone\":\"Europe/Skopje\"}")]
        [InlineData("{\"token\":\"some-token\",\"name\":\"Cvetan\",\"timeZone\":\"Europe/Skopje\"}")]
        [InlineData("{\"token\":\"some-token\",\"name\":\"Cvetan\",\"language\":\"en\"}")]
        [InlineData("")]
        public async Task JoinAsGuest_MissingTokenLanguageTimeZoneOrBody_Answers400AndCreatesNoUser(string json)
        {
            GuestScene scene = await CreateSceneAsync();
            int usersBefore = await _app.CountUsersAsync();

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(scene.Guest, GroupRequests.JoinAsGuestPath, json);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(usersBefore, await _app.CountUsersAsync());
        }

        [Fact]
        public async Task JoinAsGuest_BySomeoneSignedInWithAnAccount_Answers409GuestJoinSignedInAndCreatesNoGuest()
        {
            GuestScene scene = await CreateSceneAsync();
            SignedInUser outsider = await _app.SignUpAsync("Cvetan");
            int guestsBefore = await CountGuestsAsync();
            string before = await GroupRows.SnapshotAsync(_app, scene.Setup.GroupId);

            HttpResponseMessage response = await GroupRequests.JoinAsGuestAsync(outsider.Client, scene.Setup.InviteToken, claimMemberId: scene.FreeNameRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.GUEST_JOIN_SIGNED_IN);
            Assert.Equal(guestsBefore, await CountGuestsAsync());
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, scene.Setup.GroupId));
            Assert.Equal(outsider.UserId, await GuestIdAsync(outsider.Client));
        }

        [Fact]
        public async Task JoinAsGuest_ByAGuestWhoIsAlreadySignedIn_Answers409GuestJoinSignedInAndCreatesNoSecondGuest()
        {
            GuestScene scene = await CreateSceneAsync();
            await AssertOkAsync(GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: NewName));
            Guid firstGuestId = await GuestIdAsync(scene.Guest);
            int guestsBefore = await CountGuestsAsync();

            HttpResponseMessage response = await GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: scene.FreeNameRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.GUEST_JOIN_SIGNED_IN);
            Assert.Equal(guestsBefore, await CountGuestsAsync());
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, scene.FreeNameRowId, "user_id"));
            Assert.Equal(firstGuestId, await GuestIdAsync(scene.Guest));
        }

        [Fact]
        public async Task JoinAsGuest_BySomeoneSignedInWithAnInvalidChoice_Answers409BeforeTheChoiceIsChecked()
        {
            GuestScene scene = await CreateSceneAsync();

            HttpResponseMessage response = await GroupRequests.JoinAsGuestAsync(scene.Setup.Member.Client, scene.Setup.InviteToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.GUEST_JOIN_SIGNED_IN);
        }

        private async Task<GuestScene> CreateSceneAsync()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid freeNameRowId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, FreeName, setup.Owner.UserId);

            return new GuestScene(setup, freeNameRowId, _app.CreateClient());
        }

        private static Task<HttpResponseMessage> JoinAsync(GuestScene scene, bool tapsAFreeName)
        {
            return tapsAFreeName
                ? GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, claimMemberId: scene.FreeNameRowId)
                : GroupRequests.JoinAsGuestAsync(scene.Guest, scene.Setup.InviteToken, name: NewName);
        }

        private Task SetStatusAsync(Guid groupId, string status)
        {
            string finishedAt = status == "Finished" ? "now()" : "NULL";

            return _app.ExecuteAsync(
                $"UPDATE groups SET status = @status, closing_started_at = now(), finished_at = {finishedAt} WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("status", status));
        }

        private async Task AssertRefusedWithoutAGuestAsync(GuestScene scene, Func<Task<HttpResponseMessage>> join, HttpStatusCode expectedStatus, string expectedCode)
        {
            int guestsBefore = await CountGuestsAsync();
            int usageEventsBefore = await _app.CountUsageEventsAsync();

            HttpResponseMessage response = await join();

            await ProblemResponse.AssertAsync(response, expectedStatus, expectedCode);
            Assert.Null(AuthCookie.Find(response));
            Assert.Equal(guestsBefore, await CountGuestsAsync());
            Assert.Equal(usageEventsBefore, await _app.CountUsageEventsAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, (await AuthRequests.GetMeAsync(scene.Guest)).StatusCode);
        }

        private async Task<int> CountGuestsAsync()
        {
            List<string?> counts = await _app.QueryAsync("SELECT count(*)::text FROM users WHERE is_guest");

            return int.Parse(Assert.Single(counts)!);
        }

        private static async Task AssertOkAsync(Task<HttpResponseMessage> request)
        {
            HttpResponseMessage response = await request;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static async Task<JsonElement> MeOfAsync(HttpClient client)
        {
            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        private static async Task<Guid> GuestIdAsync(HttpClient client)
        {
            return (await MeOfAsync(client)).GetProperty("id").GetGuid();
        }
    }
}
