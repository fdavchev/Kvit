using System.Net;
using System.Text.Json;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class GoogleLogInTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const string NewPictureUrl = "https://example.com/pictures/new.png";

        [Fact]
        public async Task GoogleLogIn_LinkedAccount_Answers200WithTheAccountFieldsAndNoPassword()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            Guid userId = (await _app.FindUserAsync(identity.Email)).Id;
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(client, _app.AddGoogleToken(identity));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(userId, body.GetProperty("id").GetGuid());
            Assert.Equal("Ana", body.GetProperty("displayName").GetString());
            Assert.Equal(identity.Email, body.GetProperty("email").GetString());
            Assert.Equal("en", body.GetProperty("language").GetString());
            Assert.Equal("Europe/Skopje", body.GetProperty("timeZone").GetString());
            Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
            Assert.False(body.GetProperty("hasPassword").GetBoolean());
        }

        [Fact]
        public async Task GoogleLogIn_LinkedAccount_SetsTheLoginCookie()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(client, _app.AddGoogleToken(identity));

            Assert.NotNull(AuthCookie.Find(response));
        }

        [Fact]
        public async Task GoogleLogIn_AutomaticTimeZone_TakesTheTimeZoneSentAtLogIn()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(client, _app.AddGoogleToken(identity), "America/New_York");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("America/New_York", (await _app.FindUserAsync(identity.Email)).TimeZone);
        }

        [Fact]
        public async Task GoogleLogIn_ManualTimeZone_KeepsTheStoredTimeZone()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            await _app.MarkTimeZoneManualAsync(identity.Email);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(client, _app.AddGoogleToken(identity), "America/New_York");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Europe/Skopje", (await _app.FindUserAsync(identity.Email)).TimeZone);
        }

        [Fact]
        public async Task GoogleLogIn_ChangedPicture_RefreshesTheStoredPictureUrl()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            HttpClient client = _app.CreateClient();
            string token = _app.AddGoogleToken(identity with { PictureUrl = NewPictureUrl });

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(client, token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(NewPictureUrl, (await _app.FindUserAsync(identity.Email)).GooglePictureUrl);
        }

        [Fact]
        public async Task GoogleLogIn_LinkedAccountLockedOutByWrongPasswords_StillSignsIn()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient googleClient = await _app.CreateGoogleSignedUpClientAsync(identity);
            HttpResponseMessage passwordSet = await AuthRequests.SetPasswordAsync(googleClient, RegistrationForm.ValidPassword);
            Assert.Equal(HttpStatusCode.NoContent, passwordSet.StatusCode);
            HttpResponseMessage locked = await AuthRequests.LockByWrongLogInsAsync(_app.CreateClient(), identity.Email);
            await ProblemResponse.AssertAsync(locked, HttpStatusCode.Forbidden, ResultCodes.AUTH_LOCKED_OUT);

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(identity));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(AuthCookie.Find(response));
        }

        [Fact]
        public async Task GoogleLogIn_UnknownToken_Answers401TokenInvalidAndSetsNoCookie()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(client, "a-token-nobody-registered");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_GOOGLE_TOKEN_INVALID);
            Assert.Null(AuthCookie.Find(response));
        }

        [Fact]
        public async Task GoogleLogIn_UnverifiedEmailOfAnExistingAccount_Answers400NotVerifiedBeforeLookingUpTheAccount()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            GoogleIdentity identity = GoogleIdentities.Verified() with { Email = form.Email, EmailVerified = false };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(client, _app.AddGoogleToken(identity));

            await _app.AssertGoogleRejectedAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_GOOGLE_EMAIL_NOT_VERIFIED, identity);
        }

        [Theory]
        [InlineData("Mars/Olympus")]
        [InlineData("")]
        public async Task GoogleLogIn_InvalidTimeZone_Answers400AndKeepsTheStoredTimeZone(string timeZone)
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(client, _app.AddGoogleToken(identity), timeZone);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.TIME_ZONE_INVALID);
            Assert.Null(AuthCookie.Find(response));
            Assert.Equal("Europe/Skopje", (await _app.FindUserAsync(identity.Email)).TimeZone);
        }

        [Fact]
        public async Task GoogleLogIn_EmailOfAnotherAccountInAnotherCase_Answers400EmailTakenAndLinksNothing()
        {
            RegistrationForm form = RegistrationForm.Valid() with { Email = $"Ana.{Guid.NewGuid():N}@Example.com" };
            await _app.CreateAccountAsync(form);
            GoogleIdentity identity = GoogleIdentities.Verified() with { Email = form.Email.ToLowerInvariant() };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(client, _app.AddGoogleToken(identity));

            await _app.AssertGoogleRejectedAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_GOOGLE_EMAIL_TAKEN, identity);
        }

        [Fact]
        public async Task GoogleLogIn_NoLinkAndNoAccountWithTheEmail_Answers404NoAccountAndCreatesNothing()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(client, _app.AddGoogleToken(identity));

            await _app.AssertGoogleRejectedAsync(response, HttpStatusCode.NotFound, ResultCodes.AUTH_GOOGLE_NO_ACCOUNT, identity);
            Assert.Equal(0, await _app.CountUsersWithEmailAsync(identity.Email));
        }
    }
}
