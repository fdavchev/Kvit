using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public static class DashboardRequests
    {
        public const string DashboardPath = "/api/dashboard";
        public const string ActivePath = "/api/me/active";

        public static Task<HttpResponseMessage> GetAsync(HttpClient client)
        {
            return client.GetAsync(DashboardPath, TestContext.Current.CancellationToken);
        }

        public static async Task<JsonElement> ReadAsync(SignedInUser viewer)
        {
            HttpResponseMessage response = await GetAsync(viewer.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        public static Task<HttpResponseMessage> ActiveAsync(HttpClient client)
        {
            return client.PostAsync(ActivePath, null, TestContext.Current.CancellationToken);
        }
    }
}
