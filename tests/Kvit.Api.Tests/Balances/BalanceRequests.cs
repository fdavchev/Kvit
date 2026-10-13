using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public static class BalanceRequests
    {
        public static string PathOf(Guid groupId)
        {
            return PathOfSegment(groupId.ToString());
        }

        public static string PathOfSegment(string groupSegment)
        {
            return $"/api/groups/{groupSegment}/balances";
        }

        public static Task<HttpResponseMessage> GetAsync(HttpClient client, Guid groupId)
        {
            return client.GetAsync(PathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> GetSegmentAsync(HttpClient client, string groupSegment)
        {
            return client.GetAsync(PathOfSegment(groupSegment), TestContext.Current.CancellationToken);
        }

        public static async Task<JsonElement> ReadBalancedAsync(HttpClient client, Guid groupId)
        {
            HttpResponseMessage response = await GetAsync(client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            BalanceJson.AssertEveryCurrencySumsToZero(body);
            return body;
        }

        public static async Task<string> ReadTextAsync(HttpClient client, Guid groupId)
        {
            HttpResponseMessage response = await GetAsync(client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }
    }
}
