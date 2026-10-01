using System.Net;
using System.Text.Json;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class LogInTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        [Fact]
        public async Task LogIn_RightPassword_Answers200WithTheAccountFields()
        {
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = "Ана Петровска", Language = "mk" };
            Guid userId = await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, form.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(userId, body.GetProperty("id").GetGuid());
            Assert.Equal("Ана Петровска", body.GetProperty("displayName").GetString());
            Assert.Equal(form.Email, body.GetProperty("email").GetString());
            Assert.Equal("mk", body.GetProperty("language").GetString());
            Assert.Equal("Europe/Skopje", body.GetProperty("timeZone").GetString());
            Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
        }

        [Fact]
        public async Task LogIn_RightPassword_SetsTheLoginCookie()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, form.Password);

            Assert.NotNull(AuthCookie.Find(response));
        }

        [Fact]
        public async Task LogIn_WrongPassword_Answers401InvalidCredentials()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, AuthRequests.WrongPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
        }

        [Fact]
        public async Task LogIn_PasswordInAnotherCase_Answers401InvalidCredentials()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, form.Password.ToLowerInvariant());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
        }

        [Fact]
        public async Task LogIn_UnknownEmail_Answers401InvalidCredentials()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, RegistrationForm.UniqueEmail(), RegistrationForm.ValidPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
        }

        [Fact]
        public async Task LogIn_UnknownEmailAndWrongPassword_GiveTheSameAnswer()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage wrongPassword = await AuthRequests.LogInAsync(client, form.Email, AuthRequests.WrongPassword);
            HttpResponseMessage unknownEmail = await AuthRequests.LogInAsync(client, RegistrationForm.UniqueEmail(), AuthRequests.WrongPassword);

            JsonElement wrongPasswordBody = await AuthRequests.ReadJsonAsync(wrongPassword);
            JsonElement unknownEmailBody = await AuthRequests.ReadJsonAsync(unknownEmail);
            string[] comparedProperties = ["status", "errorCode", "title", "detail", "type"];
            Assert.Equal(wrongPassword.StatusCode, unknownEmail.StatusCode);
            foreach (string propertyName in comparedProperties)
            {
                Assert.Equal(
                    AuthRequests.ReadOptionalString(wrongPasswordBody, propertyName),
                    AuthRequests.ReadOptionalString(unknownEmailBody, propertyName));
            }
        }

        [Fact]
        public async Task LogIn_WrongPassword_SetsNoCookie()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, AuthRequests.WrongPassword);

            Assert.Null(AuthCookie.Find(response));
        }

        [Fact]
        public async Task LogIn_EmailInAnotherCase_Works()
        {
            RegistrationForm form = RegistrationForm.Valid() with { Email = $"Ana.{Guid.NewGuid():N}@Example.com" };
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email.ToLowerInvariant(), form.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task LogIn_EmailWithSurroundingSpaces_Works()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, $"  {form.Email}  ", form.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task LogIn_AutomaticTimeZone_TakesTheTimeZoneSentAtLogIn()
        {
            RegistrationForm form = RegistrationForm.Valid() with { TimeZone = "Europe/Skopje" };
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, form.Password, "America/New_York");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("America/New_York", (await _app.FindUserAsync(form.Email)).TimeZone);
        }

        [Fact]
        public async Task LogIn_ManualTimeZone_KeepsTheStoredTimeZone()
        {
            RegistrationForm form = RegistrationForm.Valid() with { TimeZone = "Europe/Skopje" };
            await _app.CreateAccountAsync(form);
            await _app.MarkTimeZoneManualAsync(form.Email);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, form.Password, "America/New_York");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Europe/Skopje", (await _app.FindUserAsync(form.Email)).TimeZone);
        }

        [Theory]
        [InlineData("Mars/Olympus")]
        [InlineData("")]
        public async Task LogIn_InvalidTimeZone_Answers400AndSetsNoCookie(string timeZone)
        {
            RegistrationForm form = RegistrationForm.Valid() with { TimeZone = "Europe/Skopje" };
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, form.Password, timeZone);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.TIME_ZONE_INVALID);
            Assert.Null(AuthCookie.Find(response));
            Assert.Equal("Europe/Skopje", (await _app.FindUserAsync(form.Email)).TimeZone);
        }

        [Fact]
        public async Task LogIn_DoesNotChangeTheStoredLanguage()
        {
            RegistrationForm form = RegistrationForm.Valid() with { Language = "mk" };
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, form.Password);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("mk", body.GetProperty("language").GetString());
            AppUser user = await _app.FindUserAsync(form.Email);
            Assert.Equal("mk", user.Language);
        }
    }
}
