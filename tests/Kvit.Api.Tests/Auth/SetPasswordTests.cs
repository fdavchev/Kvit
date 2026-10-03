using System.Net;
using System.Text.Json;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class SetPasswordTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const string NewPassword = "Novo-lozinka7";

        [Fact]
        public async Task SetPassword_GoogleOnlyAccountWithAValidPassword_Answers204WithNoBody()
        {
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(GoogleIdentities.Verified());

            HttpResponseMessage response = await AuthRequests.SetPasswordAsync(client, NewPassword);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task SetPassword_GoogleOnlyAccount_ThenLogInWithEmailAndThePassword_Answers200()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(identity);
            await AuthRequests.SetPasswordAsync(client, NewPassword);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), identity.Email, NewPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True((await AuthRequests.ReadJsonAsync(response)).GetProperty("hasPassword").GetBoolean());
        }

        [Fact]
        public async Task SetPassword_GoogleOnlyAccount_ThenMeShowsHasPassword()
        {
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(GoogleIdentities.Verified());
            await AuthRequests.SetPasswordAsync(client, NewPassword);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.True(body.GetProperty("hasPassword").GetBoolean());
        }

        [Theory]
        [InlineData("Sh0rt")]
        [InlineData("nouppercase1")]
        [InlineData("NoDigitsHere")]
        public async Task SetPassword_NewPasswordBreaksTheRule_Answers400TooWeakAndSetsNoPassword(string weakPassword)
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(identity);

            HttpResponseMessage response = await AuthRequests.SetPasswordAsync(client, weakPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_PASSWORD_TOO_WEAK);
            Assert.Null((await _app.FindUserAsync(identity.Email)).PasswordHash);
        }

        [Fact]
        public async Task SetPassword_AccountThatAlreadyHasAPassword_Answers400AlreadySetAndKeepsThePassword()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            string? hashBefore = (await _app.FindUserAsync(form.Email)).PasswordHash;

            HttpResponseMessage response = await AuthRequests.SetPasswordAsync(client, NewPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_PASSWORD_ALREADY_SET);
            Assert.Equal(hashBefore, (await _app.FindUserAsync(form.Email)).PasswordHash);
        }

        [Fact]
        public async Task SetPassword_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.SetPasswordAsync(client, NewPassword);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
