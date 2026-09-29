using System.Net;
using System.Text.Json;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class CookieTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const int LoginDays = 90;

        [Theory]
        [InlineData("register")]
        [InlineData("login")]
        public async Task Cookie_IsNamedKvitAuth(string endpoint)
        {
            HttpResponseMessage response = await AuthenticateAsync(endpoint);

            Assert.NotNull(AuthCookie.Find(response));
        }

        [Theory]
        [InlineData("register")]
        [InlineData("login")]
        public async Task Cookie_IsHttpOnly(string endpoint)
        {
            HttpResponseMessage response = await AuthenticateAsync(endpoint);

            SetCookieHeaderValue? cookie = AuthCookie.Find(response);
            Assert.NotNull(cookie);
            Assert.True(cookie.HttpOnly);
        }

        [Theory]
        [InlineData("register")]
        [InlineData("login")]
        public async Task Cookie_IsSecure(string endpoint)
        {
            HttpResponseMessage response = await AuthenticateAsync(endpoint);

            SetCookieHeaderValue? cookie = AuthCookie.Find(response);
            Assert.NotNull(cookie);
            Assert.True(cookie.Secure);
        }

        [Theory]
        [InlineData("register")]
        [InlineData("login")]
        public async Task Cookie_IsSameSiteLax(string endpoint)
        {
            HttpResponseMessage response = await AuthenticateAsync(endpoint);

            SetCookieHeaderValue? cookie = AuthCookie.Find(response);
            Assert.NotNull(cookie);
            Assert.Equal(SameSiteMode.Lax, cookie.SameSite);
        }

        [Theory]
        [InlineData("register")]
        [InlineData("login")]
        public async Task Cookie_IsSentForThePathRoot(string endpoint)
        {
            HttpResponseMessage response = await AuthenticateAsync(endpoint);

            SetCookieHeaderValue? cookie = AuthCookie.Find(response);
            Assert.NotNull(cookie);
            Assert.Equal("/", cookie.Path.Value);
        }

        [Theory]
        [InlineData("register")]
        [InlineData("login")]
        public async Task Cookie_ExpiresAbout90DaysAhead(string endpoint)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            HttpResponseMessage response = await AuthenticateAsync(endpoint);

            SetCookieHeaderValue? cookie = AuthCookie.Find(response);
            Assert.NotNull(cookie);
            DateTimeOffset expiresAt = AuthCookie.ExpiresAt(cookie, now) ?? throw new InvalidOperationException("The kvit_auth cookie has neither Expires nor Max-Age, so it would end with the browser session.");
            Assert.InRange(expiresAt - now, TimeSpan.FromDays(LoginDays - 1), TimeSpan.FromDays(LoginDays + 1));
        }

        [Fact]
        public async Task Register_LogsTheNewUserIn()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = _app.CreateClient();
            HttpResponseMessage registered = await AuthRequests.RegisterAsync(client, form);
            Guid userId = (await AuthRequests.ReadJsonAsync(registered)).GetProperty("id").GetGuid();

            HttpResponseMessage me = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(me);
            Assert.Equal(userId, body.GetProperty("id").GetGuid());
            Assert.Equal(form.Email, body.GetProperty("email").GetString());
        }

        [Fact]
        public async Task LogIn_LogsTheUserIn()
        {
            RegistrationForm form = RegistrationForm.Valid();
            Guid userId = await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();
            await AuthRequests.LogInAsync(client, form.Email, form.Password);

            HttpResponseMessage me = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(me);
            Assert.Equal(userId, body.GetProperty("id").GetGuid());
        }

        [Fact]
        public async Task LogOut_EndsTheSession()
        {
            HttpClient client = _app.CreateClient();
            await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());

            HttpResponseMessage logOut = await AuthRequests.LogOutAsync(client);
            HttpResponseMessage me = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.NoContent, logOut.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        }

        [Fact]
        public async Task LogOut_ClearsTheCookie()
        {
            HttpClient client = _app.CreateClient();
            await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());
            DateTimeOffset now = DateTimeOffset.UtcNow;

            HttpResponseMessage logOut = await AuthRequests.LogOutAsync(client);

            SetCookieHeaderValue? cookie = AuthCookie.Find(logOut);
            Assert.NotNull(cookie);
            DateTimeOffset expiresAt = AuthCookie.ExpiresAt(cookie, now) ?? throw new InvalidOperationException("The clearing kvit_auth cookie has neither Expires nor Max-Age.");
            Assert.True(expiresAt <= now, $"The clearing cookie should already be expired, but it expires at {expiresAt:O}.");
        }

        [Fact]
        public async Task LogOut_WhenNotLoggedIn_Answers204()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogOutAsync(client);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task LogOut_Twice_Answers204BothTimes()
        {
            HttpClient client = _app.CreateClient();
            await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());

            HttpResponseMessage first = await AuthRequests.LogOutAsync(client);
            HttpResponseMessage second = await AuthRequests.LogOutAsync(client);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        }

        private async Task<HttpResponseMessage> AuthenticateAsync(string endpoint)
        {
            return endpoint switch
            {
                "register" => await RegisterResponseAsync(),
                "login" => await LogInResponseAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Expected 'register' or 'login'."),
            };
        }

        private async Task<HttpResponseMessage> RegisterResponseAsync()
        {
            HttpClient client = _app.CreateClient();

            return await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());
        }

        private async Task<HttpResponseMessage> LogInResponseAsync()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            return await AuthRequests.LogInAsync(client, form.Email, form.Password);
        }
    }
}
