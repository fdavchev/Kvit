using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public static class AdminStatsRequests
    {
        public const string Path = "/api/admin/stats";

        public static Task<HttpResponseMessage> GetAsync(HttpClient client, string? from = null, string? to = null, string? by = null)
        {
            List<string> query = [];
            if (from is not null)
            {
                query.Add($"from={from}");
            }

            if (to is not null)
            {
                query.Add($"to={to}");
            }

            if (by is not null)
            {
                query.Add($"by={by}");
            }

            string url = query.Count == 0 ? Path : $"{Path}?{string.Join("&", query)}";

            return client.GetAsync(url, TestContext.Current.CancellationToken);
        }

        public static async Task<JsonElement> ReadAsync(HttpClient client, string? from = null, string? to = null, string? by = null)
        {
            HttpResponseMessage response = await GetAsync(client, from, to, by);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }
    }
}
