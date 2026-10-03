using System.Net;
using System.Text.Json;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class MeTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        [Fact]
        public async Task Me_LoggedIn_AnswersExactlyTheSevenAccountFields()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            string[] propertyNames = [.. body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
            string[] expectedNames = ["displayName", "email", "hasPassword", "id", "language", "mustChangePassword", "timeZone"];
            Assert.Equal(expectedNames, propertyNames);
        }

        [Fact]
        public async Task Me_LoggedIn_AnswersTheStoredAccountValues()
        {
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = "Ана Петровска", Language = "mk", TimeZone = "America/New_York" };
            Guid userId = await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();
            await AuthRequests.LogInAsync(client, form.Email, form.Password, "America/New_York");

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(userId, body.GetProperty("id").GetGuid());
            Assert.Equal("Ана Петровска", body.GetProperty("displayName").GetString());
            Assert.Equal(form.Email, body.GetProperty("email").GetString());
            Assert.Equal("mk", body.GetProperty("language").GetString());
            Assert.Equal("America/New_York", body.GetProperty("timeZone").GetString());
            Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
            Assert.True(body.GetProperty("hasPassword").GetBoolean());
        }

        [Fact]
        public async Task Me_TwoUsersOnTwoClients_EachSeeOnlyTheirOwnAccount()
        {
            RegistrationForm first = RegistrationForm.Valid();
            RegistrationForm second = RegistrationForm.Valid();
            HttpClient firstClient = await _app.CreateRegisteredClientAsync(first);
            HttpClient secondClient = await _app.CreateRegisteredClientAsync(second);

            HttpResponseMessage firstResponse = await AuthRequests.GetMeAsync(firstClient);
            HttpResponseMessage secondResponse = await AuthRequests.GetMeAsync(secondClient);

            Assert.Equal(first.Email, (await AuthRequests.ReadJsonAsync(firstResponse)).GetProperty("email").GetString());
            Assert.Equal(second.Email, (await AuthRequests.ReadJsonAsync(secondResponse)).GetProperty("email").GetString());
        }

        [Fact]
        public async Task Me_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Me_WithACookieThatIsNotAValidLogin_Answers401()
        {
            HttpClient client = _app.CreateClientWithoutCookies();
            using HttpRequestMessage request = new(HttpMethod.Get, "/api/me");
            request.Headers.Add("Cookie", $"{AuthCookie.Name}=not-a-real-login-cookie");

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("en", "mk")]
        [InlineData("mk", "en")]
        [InlineData("en", "en")]
        public async Task ChangeLanguage_SupportedLanguage_Answers204AndStoresIt(string startLanguage, string newLanguage)
        {
            RegistrationForm form = RegistrationForm.Valid() with { Language = startLanguage };
            HttpClient client = await _app.CreateRegisteredClientAsync(form);

            HttpResponseMessage response = await AuthRequests.ChangeLanguageAsync(client, newLanguage);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(newLanguage, (await _app.FindUserAsync(form.Email)).Language);
        }

        [Fact]
        public async Task ChangeLanguage_ThenMe_AnswersTheNewLanguage()
        {
            RegistrationForm form = RegistrationForm.Valid() with { Language = "en" };
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            await AuthRequests.ChangeLanguageAsync(client, "mk");

            HttpResponseMessage me = await AuthRequests.GetMeAsync(client);

            JsonElement body = await AuthRequests.ReadJsonAsync(me);
            Assert.Equal("mk", body.GetProperty("language").GetString());
        }

        [Theory]
        [InlineData("de")]
        [InlineData("EN")]
        [InlineData("")]
        public async Task ChangeLanguage_UnsupportedLanguage_Answers400AndKeepsTheStoredLanguage(string language)
        {
            RegistrationForm form = RegistrationForm.Valid() with { Language = "en" };
            HttpClient client = await _app.CreateRegisteredClientAsync(form);

            HttpResponseMessage response = await AuthRequests.ChangeLanguageAsync(client, language);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.LANGUAGE_INVALID);
            Assert.Equal("en", (await _app.FindUserAsync(form.Email)).Language);
        }

        [Fact]
        public async Task ChangeLanguage_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.ChangeLanguageAsync(client, "mk");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
