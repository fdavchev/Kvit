using System.Net.Http.Json;
using System.Text;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.BudgetShares
{
    public static class BudgetShareRequests
    {
        public static string SwitchPathOf(Guid groupId)
        {
            return $"{GroupRequests.MembersPathOf(groupId)}/me/counts-in-budget";
        }

        public static Task<HttpResponseMessage> SetAsync(HttpClient client, Guid groupId, bool countsInBudget)
        {
            return client.PutAsJsonAsync(SwitchPathOf(groupId), new { countsInBudget }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SetWithJsonTextAsync(HttpClient client, Guid groupId, string json)
        {
            StringContent content = new(json, Encoding.UTF8, "application/json");

            return client.PutAsync(SwitchPathOf(groupId), content, TestContext.Current.CancellationToken);
        }
    }
}
