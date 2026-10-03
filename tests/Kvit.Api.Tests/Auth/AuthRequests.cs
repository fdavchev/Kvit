using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public static class AuthRequests
    {
        public const string DefaultTimeZone = "Europe/Skopje";
        public const string WrongPassword = "Wrong-password-1";

        public const string RegisterPath = "/api/auth/register";
        public const string GoogleLogInPath = "/api/auth/google";
        public const string GoogleSignUpPath = "/api/auth/google/sign-up";

        public static string RegistrationJson(RegistrationForm form)
        {
            return JsonSerializer.Serialize(new
            {
                displayName = form.DisplayName,
                email = form.Email,
                password = form.Password,
                timeZone = form.TimeZone,
                language = form.Language,
            });
        }

        public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, RegistrationForm form)
        {
            return PostJsonAsync(client, RegisterPath, RegistrationJson(form));
        }

        public static Task<HttpResponseMessage> LogInAsync(HttpClient client, string email, string password, string timeZone = DefaultTimeZone)
        {
            string json = JsonSerializer.Serialize(new { email, password, timeZone });

            return PostJsonAsync(client, "/api/auth/login", json);
        }

        public static Task<HttpResponseMessage> LogOutAsync(HttpClient client)
        {
            return client.PostAsync("/api/auth/logout", null, TestContext.Current.CancellationToken);
        }

        public static async Task<HttpResponseMessage> LockByWrongLogInsAsync(HttpClient client, string email)
        {
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                HttpResponseMessage response = await LogInAsync(client, email, WrongPassword);

                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }

            return await LogInAsync(client, email, WrongPassword);
        }

        public static Task<HttpResponseMessage> ChangePasswordAsync(HttpClient client, string currentPassword, string newPassword)
        {
            string json = JsonSerializer.Serialize(new { currentPassword, newPassword });

            return PostJsonAsync(client, "/api/auth/change-password", json);
        }

        public static Task<HttpResponseMessage> GoogleLogInAsync(HttpClient client, string idToken, string timeZone = DefaultTimeZone)
        {
            string json = JsonSerializer.Serialize(new { idToken, timeZone });

            return PostJsonAsync(client, GoogleLogInPath, json);
        }

        public static Task<HttpResponseMessage> GoogleSignUpAsync(
            HttpClient client,
            string idToken,
            string displayName = "Ana",
            string timeZone = DefaultTimeZone,
            string language = "en")
        {
            string json = JsonSerializer.Serialize(new { idToken, displayName, timeZone, language });

            return PostJsonAsync(client, GoogleSignUpPath, json);
        }

        public static Task<HttpResponseMessage> SetPasswordAsync(HttpClient client, string newPassword)
        {
            string json = JsonSerializer.Serialize(new { newPassword });

            return PostJsonAsync(client, "/api/auth/set-password", json);
        }

        public static Task<HttpResponseMessage> GetMeAsync(HttpClient client)
        {
            return client.GetAsync("/api/me", TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ChangeLanguageAsync(HttpClient client, string language)
        {
            string json = JsonSerializer.Serialize(new { language });
            StringContent content = new(json, Encoding.UTF8, "application/json");

            return client.PutAsync("/api/me/language", content, TestContext.Current.CancellationToken);
        }

        public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using JsonDocument document = JsonDocument.Parse(body);

            return document.RootElement.Clone();
        }

        public static string? ReadOptionalString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) ? value.ToString() : null;
        }

        private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, string json)
        {
            StringContent content = new(json, Encoding.UTF8, "application/json");

            return client.PostAsync(path, content, TestContext.Current.CancellationToken);
        }
    }
}
