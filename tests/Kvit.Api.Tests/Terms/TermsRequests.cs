using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Xunit;

namespace Kvit.Api.Tests.Terms
{
    public static class TermsRequests
    {
        public const string CurrentVersion = "2026-11-04";
        public const string OlderVersion = "2025-01-01";
        public const string AcceptPath = "/api/me/terms/accept";

        public static Task<HttpResponseMessage> AcceptAsync(HttpClient client)
        {
            return client.PostAsync(AcceptPath, null, TestContext.Current.CancellationToken);
        }

        public static TermsFields ReadFields(JsonElement body)
        {
            JsonElement version = Property(body, "termsVersion");
            JsonElement upToDate = Property(body, "termsUpToDate");
            JsonElement updateIsMaterial = Property(body, "termsUpdateIsMaterial");

            return new TermsFields(
                version.ValueKind == JsonValueKind.Null ? null : version.GetString(),
                upToDate.GetBoolean(),
                updateIsMaterial.GetBoolean());
        }

        public static async Task<TermsFields> ReadFieldsAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return ReadFields(await AuthRequests.ReadJsonAsync(response));
        }

        private static JsonElement Property(JsonElement body, string name)
        {
            Assert.True(body.TryGetProperty(name, out JsonElement value), $"The response has no '{name}' property: {body}");

            return value;
        }
    }
}
