using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public static class SettlementEndpoints
    {
        public const string SettlementPlaceholder = "{settlement}";

        public static TheoryData<string, string> All => new()
        {
            { "GET", "/settlements" },
            { "POST", "/settlements" },
            { "POST", "/settlements/{settlement}/confirm" },
            { "POST", "/settlements/{settlement}/reject" },
            { "POST", "/settlements/{settlement}/cancel" },
            { "DELETE", "/settlements/{settlement}" },
            { "POST", "/settlements/{settlement}/restore" },
        };

        public static TheoryData<string, string> WithSettlementId => new()
        {
            { "POST", "/settlements/{settlement}/confirm" },
            { "POST", "/settlements/{settlement}/reject" },
            { "POST", "/settlements/{settlement}/cancel" },
            { "DELETE", "/settlements/{settlement}" },
            { "POST", "/settlements/{settlement}/restore" },
        };

        public static string PathFor(string groupSegment, string suffix, string settlementSegment)
        {
            return $"/api/groups/{groupSegment}{suffix.Replace(SettlementPlaceholder, settlementSegment)}";
        }

        public static async Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string suffix, Guid groupId, Guid settlementId)
        {
            return await SendToSegmentsAsync(client, method, suffix, groupId.ToString(), settlementId.ToString());
        }

        public static async Task<HttpResponseMessage> SendToSegmentsAsync(HttpClient client, string method, string suffix, string groupSegment, string settlementSegment)
        {
            string path = PathFor(groupSegment, suffix, settlementSegment);
            object body = SettlementRequests.RecordBody(Guid.CreateVersion7(), Guid.CreateVersion7(), SettlementRequests.DefaultAmountMinor, "MKD", Guid.CreateVersion7());

            return await GroupRequests.SendAsync(client, method, path, body);
        }
    }
}
