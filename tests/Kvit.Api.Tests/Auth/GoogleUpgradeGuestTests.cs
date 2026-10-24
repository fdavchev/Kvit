using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class GoogleUpgradeGuestTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string FreeName = "Marko";

        [Fact]
        public async Task GoogleUpgradeGuest_ValidToken_Answers200WithTheSameIdTheGoogleEmailThePictureAndTheGuestsOwnName()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            GoogleIdentity identity = GoogleIdentities.Verified();

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(identity));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["budgetTotalCurrency", "displayName", "email", "hasPassword", "id", "isGuest", "language", "mustChangePassword", "pictureUrl", "timeZone"], [.. body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)]);
            Assert.Equal(guest.UserId, body.GetProperty("id").GetGuid());
            Assert.Equal(identity.Email, body.GetProperty("email").GetString());
            Assert.Equal(FreeName, body.GetProperty("displayName").GetString());
            Assert.Equal(GoogleIdentities.PictureUrl, body.GetProperty("pictureUrl").GetString());
            Assert.False(body.GetProperty("isGuest").GetBoolean());
            Assert.False(body.GetProperty("hasPassword").GetBoolean());
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ValidToken_RefreshesTheLoginSoMeAnswersTheAccountWithTheGoogleEmail()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            GoogleIdentity identity = GoogleIdentities.Verified();

            HttpResponseMessage upgraded = await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(identity));

            Assert.NotNull(AuthCookie.Find(upgraded));
            HttpResponseMessage me = await AuthRequests.GetMeAsync(guest.Client);
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(me);
            Assert.Equal(guest.UserId, body.GetProperty("id").GetGuid());
            Assert.Equal(identity.Email, body.GetProperty("email").GetString());
            Assert.False(body.GetProperty("isGuest").GetBoolean());
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ValidToken_LinksExactlyOneGoogleLoginToTheGuestsOwnUser()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            GoogleIdentity identity = GoogleIdentities.Verified();
            int usersBefore = await _app.CountUsersAsync();

            await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(identity));

            Assert.Equal(guest.UserId, Assert.Single(await _app.GoogleLoginUserIdsAsync(identity.Subject)));
            Assert.Equal(usersBefore, await _app.CountUsersAsync());
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ValidToken_StoresTheEmailAsUserNameAndKeepsLanguageAndTimeZone()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync(FreeName);
            HttpClient guestClient = _app.CreateClient();
            HttpResponseMessage joined = await GroupRequests.JoinAsGuestAsync(guestClient, await GuestSteps.InviteTokenOfAsync(_app, group.GroupId), claimMemberId: group.PlainRowIds[0], language: "mk", timeZone: "America/New_York");
            Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
            GoogleIdentity identity = GoogleIdentities.Verified();

            await AuthRequests.GoogleUpgradeGuestAsync(guestClient, _app.AddGoogleToken(identity));

            AppUser user = await _app.FindUserAsync(identity.Email);
            Assert.Equal(identity.Email, user.UserName);
            Assert.Equal(identity.Email.ToUpperInvariant(), user.NormalizedEmail);
            Assert.False(user.IsGuest);
            Assert.Null(user.PasswordHash);
            Assert.Equal(FreeName, user.DisplayName);
            Assert.Equal("mk", user.Language);
            Assert.Equal("America/New_York", user.TimeZone);
            Assert.Equal(GoogleIdentities.PictureUrl, user.GooglePictureUrl);
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ValidToken_KeepsTheMemberRowOnTheSameUser()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            string groupBefore = await GroupRows.SnapshotAsync(_app, group.GroupId);

            await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(GoogleIdentities.Verified()));

            Assert.Equal(groupBefore, await GroupRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(guest.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, guest.RowId, "user_id"));
            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(guest.Client, group.GroupId)).StatusCode);
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ValidToken_ThePersonCanLogOutAndLogInAgainWithGoogle()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            GoogleIdentity identity = GoogleIdentities.Verified();
            await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(identity));
            await AuthRequests.LogOutAsync(guest.Client);

            HttpResponseMessage loggedIn = await AuthRequests.GoogleLogInAsync(guest.Client, _app.AddGoogleToken(identity));

            Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
            Assert.Equal(guest.UserId, (await AuthRequests.ReadJsonAsync(loggedIn)).GetProperty("id").GetGuid());
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ValidToken_RecordsOneGuestUpgradedEventWithTheGoogleMethod()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(GoogleIdentities.Verified()));

            List<UsageEvent> events = await _app.UsageEventsOfAsync(guest.UserId);
            UsageEvent upgraded = Assert.Single(events, usageEvent => usageEvent.Type == UsageEventType.GuestUpgraded);
            Assert.Equal("Google", upgraded.Detail);
            Assert.Single(events, usageEvent => usageEvent.Type == UsageEventType.SignedUp);
        }

        [Fact]
        public async Task GoogleUpgradeGuest_EmailOfAnotherAccount_Answers400GoogleEmailTakenLinksNothingAndTheGuestStaysAGuest()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            RegistrationForm other = RegistrationForm.Valid();
            await _app.CreateAccountAsync(other);
            GoogleIdentity identity = GoogleIdentities.Verified() with { Email = other.Email };

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(identity));

            await GuestUpgradeChecks.AssertRefusedAndStillAGuestAsync(_app, response, guest, HttpStatusCode.BadRequest, ResultCodes.AUTH_GOOGLE_EMAIL_TAKEN);
            Assert.Empty(await _app.GoogleLoginUserIdsAsync(identity.Subject));
            Assert.Equal(1, await _app.CountUsersWithEmailAsync(other.Email));
        }

        [Fact]
        public async Task GoogleUpgradeGuest_GoogleAccountAlreadyLinkedToAnotherUser_Answers400GoogleEmailTakenAndKeepsTheLinkOnTheOtherUser()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            GoogleIdentity linked = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(linked);
            Guid otherUserId = (await _app.FindUserAsync(linked.Email)).Id;
            GoogleIdentity sameGoogleAccountNewEmail = linked with { Email = RegistrationForm.UniqueEmail() };

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(sameGoogleAccountNewEmail));

            await GuestUpgradeChecks.AssertRefusedAndStillAGuestAsync(_app, response, guest, HttpStatusCode.BadRequest, ResultCodes.AUTH_GOOGLE_EMAIL_TAKEN);
            Assert.Equal(otherUserId, Assert.Single(await _app.GoogleLoginUserIdsAsync(linked.Subject)));
            Assert.Equal(0, await _app.CountUsersWithEmailAsync(sameGoogleAccountNewEmail.Email));
        }

        [Fact]
        public async Task GoogleUpgradeGuest_UnknownToken_Answers400TokenInvalidAndKeepsTheGuestSignedIn()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, "a-token-nobody-registered");

            await GuestUpgradeChecks.AssertRefusedAndStillAGuestAsync(_app, response, guest, HttpStatusCode.BadRequest, ResultCodes.AUTH_GOOGLE_TOKEN_INVALID);
        }

        [Fact]
        public async Task GoogleUpgradeGuest_UnverifiedEmail_Answers400NotVerifiedAndLinksNothing()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            GoogleIdentity identity = GoogleIdentities.Verified() with { EmailVerified = false };

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(identity));

            await GuestUpgradeChecks.AssertRefusedAndStillAGuestAsync(_app, response, guest, HttpStatusCode.BadRequest, ResultCodes.AUTH_GOOGLE_EMAIL_NOT_VERIFIED);
            Assert.Empty(await _app.GoogleLoginUserIdsAsync(identity.Subject));
        }

        [Fact]
        public async Task GoogleUpgradeGuest_NotSignedIn_Answers401AndLinksNothing()
        {
            HttpClient client = _app.CreateClient();
            GoogleIdentity identity = GoogleIdentities.Verified();

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(client, _app.AddGoogleToken(identity));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Empty(await _app.GoogleLoginUserIdsAsync(identity.Subject));
            Assert.Equal(0, await _app.CountUsersWithEmailAsync(identity.Email));
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ByAnAccountWithAPassword_Answers409NotGuestAndLinksNothing()
        {
            SignedInUser account = await _app.SignUpAsync();
            GoogleIdentity identity = GoogleIdentities.Verified();

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(account.Client, _app.AddGoogleToken(identity));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.AUTH_NOT_GUEST);
            Assert.Null(AuthCookie.Find(response));
            Assert.Empty(await _app.GoogleLoginUserIdsAsync(identity.Subject));
            Assert.Equal(account.Form.Email, await GuestUpgradeChecks.UserValueAsync(_app, account.UserId, "email"));
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ByAnAccountWithAnUnknownToken_Answers409BeforeTheTokenIsChecked()
        {
            SignedInUser account = await _app.SignUpAsync();

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(account.Client, "a-token-nobody-registered");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.AUTH_NOT_GUEST);
        }
    }
}
