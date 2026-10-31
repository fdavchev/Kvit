using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Xunit;

namespace Kvit.Api.Tests.GroupSpending
{
    public static class GroupSpendingRequests
    {
        public static string PathOf(Guid groupId)
        {
            return $"/api/groups/{groupId}/spending";
        }

        public static Task<HttpResponseMessage> GetAsync(HttpClient client, Guid groupId)
        {
            return client.GetAsync(PathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static async Task<JsonElement> ReadAsync(HttpClient client, Guid groupId)
        {
            HttpResponseMessage response = await GetAsync(client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        public static async Task<string> ReadTextAsync(HttpClient client, Guid groupId)
        {
            HttpResponseMessage response = await GetAsync(client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }
    }
}
