using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Persistence;
using Kvit.Contracts.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class MePictureUrlTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const string ChangedPictureUrl = "https://lh3.googleusercontent.com/a/changed-picture=s96-c";

        [Fact]
        public async Task Me_AccountMadeWithAPassword_AnswersANullPictureUrl()
        {
            HttpClient client = await _app.CreateRegisteredClientAsync(RegistrationForm.Valid());

            JsonElement body = await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(client));

            Assert.Equal(JsonValueKind.Null, body.GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task Me_GoogleAccountWithAPicture_AnswersTheStoredGooglePictureUrl()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(identity);

            JsonElement body = await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(client));

            Assert.Equal(GoogleIdentities.PictureUrl, body.GetProperty("pictureUrl").GetString());
        }

        [Fact]
        public async Task Me_GoogleAccountWithoutAPicture_AnswersANullPictureUrl()
        {
            GoogleIdentity identity = GoogleIdentities.Verified() with { PictureUrl = null };
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(identity);

            JsonElement body = await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(client));

            Assert.Equal(JsonValueKind.Null, body.GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task Me_PictureUrlChangedInTheDatabase_AnswersTheNewUrl()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(identity);
            Guid userId = (await _app.FindUserAsync(identity.Email)).Id;
            await UserRows.SetGooglePictureUrlAsync(_app, userId, ChangedPictureUrl);

            JsonElement body = await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(client));

            Assert.Equal(ChangedPictureUrl, body.GetProperty("pictureUrl").GetString());
        }

        [Fact]
        public async Task Me_PictureUrlClearedInTheDatabase_AnswersANullPictureUrl()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(identity);
            Guid userId = (await _app.FindUserAsync(identity.Email)).Id;
            await UserRows.SetGooglePictureUrlAsync(_app, userId, null);

            JsonElement body = await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(client));

            Assert.Equal(JsonValueKind.Null, body.GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task Register_Answer_HasANullPictureUrl()
        {
            HttpResponseMessage response = await AuthRequests.RegisterAsync(_app.CreateClient(), RegistrationForm.Valid());

            JsonElement body = await AuthRequests.ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task LogIn_Answer_HasANullPictureUrlForAPasswordAccount()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), form.Email, form.Password);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("pictureUrl").ValueKind);
        }

        [Fact]
        public async Task GoogleSignUp_Answer_HasTheGooglePictureUrl()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(_app.CreateClient(), _app.AddGoogleToken(identity));

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(GoogleIdentities.PictureUrl, body.GetProperty("pictureUrl").GetString());
        }

        [Fact]
        public async Task GoogleLogIn_Answer_HasTheStoredPictureUrl()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(identity));

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(GoogleIdentities.PictureUrl, body.GetProperty("pictureUrl").GetString());
        }
    }
}
