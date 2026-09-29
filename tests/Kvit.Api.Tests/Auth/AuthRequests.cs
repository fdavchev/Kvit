using System.Text;
using System.Text.Json;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public static class AuthRequests
    {
        public const string DefaultTimeZone = "Europe/Skopje";

        public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, RegistrationForm form)
        {
            string json = JsonSerializer.Serialize(new
            {
                displayName = form.DisplayName,
                email = form.Email,
                password = form.Password,
                timeZone = form.TimeZone,
                language = form.Language,
            });

            return PostJsonAsync(client, "/api/auth/register", json);
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
