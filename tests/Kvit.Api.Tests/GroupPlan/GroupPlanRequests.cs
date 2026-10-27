using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public static class GroupPlanRequests
    {
        public const string Mkd = "MKD";
        public const string Eur = "EUR";
        public const long LargestMkdAmount = 999_999_999_900;
        public const long LargestEurAmount = 999_999_999_999;
        public const long AboveTheBound = 1_000_000_000_000;
        public const string AmountInvalidCode = "GROUP_PLAN_AMOUNT_INVALID";

        public static string PathOf(Guid groupId)
        {
            return $"/api/groups/{groupId}/plan";
        }

        public static Task<HttpResponseMessage> SetAsync(HttpClient client, Guid groupId, long amountMinor, string currency)
        {
            return client.PutAsJsonAsync(PathOf(groupId), new { amountMinor, currency }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SetWithJsonTextAsync(HttpClient client, Guid groupId, string json)
        {
            StringContent content = new(json, Encoding.UTF8, "application/json");

            return client.PutAsync(PathOf(groupId), content, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Guid groupId)
        {
            return client.DeleteAsync(PathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ChangeAsync(HttpClient client, string change, Guid groupId, long amountMinor = 2_000_000, string currency = Mkd)
        {
            return change switch
            {
                "set" => SetAsync(client, groupId, amountMinor, currency),
                "remove" => DeleteAsync(client, groupId),
                _ => throw new ArgumentOutOfRangeException(nameof(change), change, "Not a group plan change."),
            };
        }
    }
}
