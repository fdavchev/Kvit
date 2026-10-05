using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public static class MemberEndpoints
    {
        public const string MemberPlaceholder = "{member}";

        public static TheoryData<string, string> All => new()
        {
            { "GET", "/members" },
            { "POST", "/members" },
            { "DELETE", "/members/{member}" },
            { "POST", "/members/{member}/let-back-in" },
            { "POST", "/leave" },
            { "POST", "/owner" },
            { "POST", "/members/{member}/claim" },
            { "POST", "/members/{member}/undo-claim" },
            { "POST", "/invite/reset" },
            { "POST", "/invite/undo-reset" },
        };

        public static TheoryData<string, string> WithMemberId => new()
        {
            { "DELETE", "/members/{member}" },
            { "POST", "/members/{member}/let-back-in" },
            { "POST", "/members/{member}/claim" },
            { "POST", "/members/{member}/undo-claim" },
        };

        public static TheoryData<string, string> OwnerOnly => new()
        {
            { "DELETE", "/members/{member}" },
            { "POST", "/members/{member}/let-back-in" },
            { "POST", "/owner" },
            { "POST", "/members/{member}/undo-claim" },
            { "POST", "/invite/reset" },
            { "POST", "/invite/undo-reset" },
        };

        public static string PathFor(string groupSegment, string suffix, string memberSegment)
        {
            return $"{GroupRequests.GroupsPath}/{groupSegment}{suffix.Replace(MemberPlaceholder, memberSegment)}";
        }

        public static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string suffix, Guid groupId, Guid memberId)
        {
            string path = PathFor(groupId.ToString(), suffix, memberId.ToString());

            return GroupRequests.SendAsync(client, method, path, new { name = "Zed", memberId });
        }
    }
}
