using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public static class ClosingEndpoints
    {
        public static TheoryData<string, string> All => new()
        {
            { "POST", "/closing" },
            { "POST", "/closing/confirm" },
            { "POST", "/closing/object" },
            { "POST", "/unlock" },
        };

        public static string PathFor(string groupSegment, string suffix)
        {
            return $"{GroupRequests.GroupsPath}/{groupSegment}{suffix}";
        }

        public static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string suffix, Guid groupId)
        {
            return SendToSegmentAsync(client, method, suffix, groupId.ToString());
        }

        public static Task<HttpResponseMessage> SendToSegmentAsync(HttpClient client, string method, string suffix, string groupSegment)
        {
            return GroupRequests.SendAsync(client, method, PathFor(groupSegment, suffix), new { reason = (string?)null });
        }
    }
}
