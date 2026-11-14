using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public static class CleanupRequests
    {
        public const string Path = "/api/internal/cleanup";
        public const string SecretHeader = "X-Kvit-Cleanup-Secret";

        public static readonly string[] FieldNames =
        [
            "accountsPurged",
            "expensesRemoved",
            "groupsRemoved",
            "personalEntriesRemoved",
            "settlementsRemoved",
            "usageEventsDetached",
        ];

        public static Task<HttpResponseMessage> PostAsync(HttpClient client, string secret)
        {
            return PostWithHeaderAsync(client, SecretHeader, secret);
        }

        public static async Task<HttpResponseMessage> PostWithHeaderAsync(HttpClient client, string headerName, string value)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, Path);
            request.Headers.TryAddWithoutValidation(headerName, value);

            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        public static async Task<HttpResponseMessage> PostWithoutSecretAsync(HttpClient client)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, Path);

            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        public static async Task<HttpResponseMessage> PostWithSecretInTheQueryAsync(HttpClient client, string secret)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, $"{Path}?secret={Uri.EscapeDataString(secret)}");

            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        public static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string secret)
        {
            using HttpRequestMessage request = new(method, Path);
            request.Headers.TryAddWithoutValidation(SecretHeader, secret);

            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        public static async Task<CleanupResult> ReadResultAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);

            return new CleanupResult(
                body.GetProperty("accountsPurged").GetInt32(),
                body.GetProperty("groupsRemoved").GetInt32(),
                body.GetProperty("expensesRemoved").GetInt32(),
                body.GetProperty("settlementsRemoved").GetInt32(),
                body.GetProperty("personalEntriesRemoved").GetInt32(),
                body.GetProperty("usageEventsDetached").GetInt32());
        }

        public static async Task AssertEmptyAnswerAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
        {
            Assert.Equal(expectedStatus, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
    }
}
