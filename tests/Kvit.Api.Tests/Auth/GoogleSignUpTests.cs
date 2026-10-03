using System.Net;
using System.Text.Json;
using Kvit.Contracts.Auth;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class GoogleSignUpTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        [Fact]
        public async Task GoogleSignUp_ValidData_Answers200WithTheStoredAccountValuesAndNoPassword()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity), "Ана Петровска", "America/New_York", "mk");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            AppUser user = await _app.FindUserAsync(identity.Email);
            Assert.Equal(user.Id, body.GetProperty("id").GetGuid());
            Assert.Equal("Ана Петровска", body.GetProperty("displayName").GetString());
            Assert.Equal(identity.Email, body.GetProperty("email").GetString());
            Assert.Equal("mk", body.GetProperty("language").GetString());
            Assert.Equal("America/New_York", body.GetProperty("timeZone").GetString());
            Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
            Assert.False(body.GetProperty("hasPassword").GetBoolean());
        }

        [Fact]
        public async Task GoogleSignUp_ValidData_StoresTheAccountWithTheTrimmedNameThePictureAndNoPasswordHash()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = _app.CreateClient();

            await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity), "  Ана  ", "America/New_York", "mk");

            AppUser user = await _app.FindUserAsync(identity.Email);
            Assert.Equal("Ана", user.DisplayName);
            Assert.Equal("mk", user.Language);
            Assert.Equal("America/New_York", user.TimeZone);
            Assert.False(user.IsTimeZoneManual);
            Assert.Equal(GoogleIdentities.PictureUrl, user.GooglePictureUrl);
            Assert.Null(user.PasswordHash);
        }

        [Fact]
        public async Task GoogleSignUp_ValidData_CreatesExactlyOneGoogleLoginRowForTheNewUser()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = _app.CreateClient();

            await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity));

            Guid userId = (await _app.FindUserAsync(identity.Email)).Id;
            Assert.Equal(userId, Assert.Single(await _app.GoogleLoginUserIdsAsync(identity.Subject)));
        }

        [Fact]
        public async Task GoogleSignUp_ValidData_RecordsExactlyOneSignedUpEventWithTheGoogleMethod()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = _app.CreateClient();

            await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity));

            Guid userId = (await _app.FindUserAsync(identity.Email)).Id;
            UsageEvent signedUp = Assert.Single(await _app.UsageEventsOfAsync(userId));
            Assert.Equal(UsageEventType.SignedUp, signedUp.Type);
            Assert.Equal("Google", signedUp.Detail);
        }

        [Fact]
        public async Task GoogleSignUp_ValidData_SignsTheUserInSoMeAnswersTheNewAccount()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = _app.CreateClient();
            HttpResponseMessage signedUp = await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity));
            Assert.NotNull(AuthCookie.Find(signedUp));

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(identity.Email, body.GetProperty("email").GetString());
            Assert.False(body.GetProperty("hasPassword").GetBoolean());
        }

        [Fact]
        public async Task GoogleSignUp_SubjectAlreadyLinked_AnswersTheExistingAccountAndCreatesNoDuplicates()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            AppUser existing = await _app.FindUserAsync(identity.Email);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity), "Another name");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(existing.Id, body.GetProperty("id").GetGuid());
            Assert.Equal(existing.DisplayName, body.GetProperty("displayName").GetString());
            Assert.NotNull(AuthCookie.Find(response));
            Assert.Equal(1, await _app.CountUsersWithEmailAsync(identity.Email));
            Assert.Single(await _app.GoogleLoginUserIdsAsync(identity.Subject));
            Assert.Single(await _app.UsageEventsOfAsync(existing.Id));
        }

        [Fact]
        public async Task GoogleSignUp_EmailOfAnotherAccount_Answers400EmailTakenAndLinksNothing()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            GoogleIdentity identity = GoogleIdentities.Verified() with { Email = form.Email };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity));

            await _app.AssertGoogleRejectedAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_GOOGLE_EMAIL_TAKEN, identity);
            Assert.Equal(1, await _app.CountUsersWithEmailAsync(form.Email));
        }

        [Fact]
        public async Task GoogleSignUp_UnknownToken_Answers401TokenInvalidAndCreatesNoAccount()
        {
            int usersBefore = await _app.CountUsersAsync();
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(client, "a-token-nobody-registered");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_GOOGLE_TOKEN_INVALID);
            Assert.Null(AuthCookie.Find(response));
            Assert.Equal(usersBefore, await _app.CountUsersAsync());
        }

        [Fact]
        public async Task GoogleSignUp_UnverifiedEmail_Answers400NotVerifiedAndCreatesNothing()
        {
            GoogleIdentity identity = GoogleIdentities.Verified() with { EmailVerified = false };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity));

            await AssertRejectedAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_GOOGLE_EMAIL_NOT_VERIFIED, identity);
        }

        [Theory]
        [InlineData("Mars/Olympus")]
        [InlineData("")]
        public async Task GoogleSignUp_InvalidTimeZone_Answers400AndCreatesNothing(string timeZone)
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity), timeZone: timeZone);

            await AssertRejectedAsync(response, HttpStatusCode.BadRequest, ResultCodes.TIME_ZONE_INVALID, identity);
        }

        [Theory]
        [InlineData("de")]
        [InlineData("EN")]
        [InlineData("")]
        public async Task GoogleSignUp_UnsupportedLanguage_Answers400AndCreatesNothing(string language)
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity), language: language);

            await AssertRejectedAsync(response, HttpStatusCode.BadRequest, ResultCodes.LANGUAGE_INVALID, identity);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public async Task GoogleSignUp_BlankDisplayName_Answers400AndCreatesNothing(string displayName)
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(client, _app.AddGoogleToken(identity), displayName);

            await AssertRejectedAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_DISPLAY_NAME_INVALID, identity);
        }

        private async Task AssertRejectedAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedErrorCode, GoogleIdentity identity)
        {
            await _app.AssertGoogleRejectedAsync(response, expectedStatus, expectedErrorCode, identity);
            Assert.Equal(0, await _app.CountUsersWithEmailAsync(identity.Email));
        }
    }
}
