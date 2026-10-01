using System.Net;
using System.Text.Json;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class MustChangePasswordTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const string UnprotectedPath = "/api/test/unprotected";
        private const string NewPassword = "Novo-lozinka7";

        [Fact]
        public async Task Me_WithATemporaryPassword_Answers200WithMustChangePasswordTrue()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.True(body.GetProperty("mustChangePassword").GetBoolean());
        }

        [Fact]
        public async Task LogOut_WithATemporaryPassword_Answers204()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);

            HttpResponseMessage response = await AuthRequests.LogOutAsync(client);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangeLanguage_WithATemporaryPassword_Answers403MustChangePasswordAndKeepsTheLanguage()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);

            HttpResponseMessage response = await AuthRequests.ChangeLanguageAsync(client, "mk");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.AUTH_MUST_CHANGE_PASSWORD);
            Assert.Equal(account.Form.Language, (await _app.FindUserAsync(account.Form.Email)).Language);
        }

        [Fact]
        public async Task EndpointWithoutAnyAuthorizationAttribute_WithATemporaryPassword_Answers403MustChangePassword()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = _app.CreateClientWithTestControllers();
            HttpResponseMessage loggedIn = await AuthRequests.LogInAsync(client, account.Form.Email, account.TemporaryPassword);
            Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);

            HttpResponseMessage response = await client.GetAsync(UnprotectedPath, TestContext.Current.CancellationToken);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.AUTH_MUST_CHANGE_PASSWORD);
        }

        [Fact]
        public async Task ChangeLanguage_AfterTheTemporaryPasswordWasChanged_Answers204()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);
            HttpResponseMessage changed = await AuthRequests.ChangePasswordAsync(client, account.TemporaryPassword, NewPassword);
            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

            HttpResponseMessage response = await AuthRequests.ChangeLanguageAsync(client, "mk");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Me_AfterTheTemporaryPasswordWasChanged_AnswersMustChangePasswordFalse()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);
            HttpResponseMessage changed = await AuthRequests.ChangePasswordAsync(client, account.TemporaryPassword, NewPassword);
            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
        }
    }
}
