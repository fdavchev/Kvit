using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public static class OwnerToolsRequests
    {
        public const string PeoplePath = "/api/admin/tools/people";
        public const string GroupsPath = "/api/admin/tools/groups";
        public const string LogPath = "/api/admin/tools/log";

        public const string ReasonInvalidCode = "OWNER_REASON_INVALID";
        public const string PersonNotFoundCode = "OWNER_PERSON_NOT_FOUND";
        public const string CannotActOnAdminCode = "OWNER_CANNOT_ACT_ON_ADMIN";
        public const string GroupNotFoundCode = "OWNER_GROUP_NOT_FOUND";
        public const string AccountSuspendedCode = "ACCOUNT_SUSPENDED";
        public const string GroupClosedByAdminCode = "GROUP_CLOSED_BY_ADMIN";

        public const string DefaultReason = "Reported by two members";
        public const int ReasonMaxLength = 500;
        public const int LogDefaultLimit = 50;
        public const int LogMaxLimit = 200;
        public const int PeopleMaxCount = 10;

        public static readonly string[] PersonFieldNames = ExpenseJson.Sorted(
            "id", "displayName", "email", "isGuest", "isSuspended", "isAdmin", "deletionPending", "isDeleted", "createdAt", "groupCount");

        public static readonly string[] GroupFieldNames = ExpenseJson.Sorted(
            "id", "name", "emoji", "status", "memberCount", "ownerName", "createdAt", "isDeleted", "closedByAdminAt");

        public static readonly string[] LogFieldNames = ExpenseJson.Sorted(
            "id", "createdAt", "adminName", "action", "targetKind", "targetLabel", "reason");

        public static string PersonActionPathOf(Guid personId, string action)
        {
            return $"{PeoplePath}/{personId}/{action}";
        }

        public static string GroupPathOf(Guid groupId)
        {
            return $"{GroupsPath}/{groupId}";
        }

        public static string GroupActionPathOf(Guid groupId, string action)
        {
            return $"{GroupPathOf(groupId)}/{action}";
        }

        public static Task<HttpResponseMessage> PeopleAsync(HttpClient client, string? query)
        {
            string path = query is null ? PeoplePath : $"{PeoplePath}?query={Uri.EscapeDataString(query)}";

            return client.GetAsync(path, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> SuspendAsync(HttpClient client, Guid personId, string reason = DefaultReason)
        {
            return PostReasonAsync(client, PersonActionPathOf(personId, "suspend"), reason);
        }

        public static Task<HttpResponseMessage> UnsuspendAsync(HttpClient client, Guid personId, string reason = DefaultReason)
        {
            return PostReasonAsync(client, PersonActionPathOf(personId, "unsuspend"), reason);
        }

        public static Task<HttpResponseMessage> EraseNameAsync(HttpClient client, Guid personId, string reason = DefaultReason)
        {
            return PostReasonAsync(client, PersonActionPathOf(personId, "erase-name"), reason);
        }

        public static Task<HttpResponseMessage> GroupAsync(HttpClient client, Guid groupId)
        {
            return client.GetAsync(GroupPathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> CloseAsync(HttpClient client, Guid groupId, string reason = DefaultReason)
        {
            return PostReasonAsync(client, GroupActionPathOf(groupId, "close"), reason);
        }

        public static Task<HttpResponseMessage> ReopenAsync(HttpClient client, Guid groupId, string reason = DefaultReason)
        {
            return PostReasonAsync(client, GroupActionPathOf(groupId, "reopen"), reason);
        }

        public static Task<HttpResponseMessage> LogAsync(HttpClient client, int? limit = null)
        {
            string path = limit is null ? LogPath : $"{LogPath}?limit={limit}";

            return client.GetAsync(path, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> PostReasonAsync(HttpClient client, string path, string reason)
        {
            return GroupRequests.PostJsonTextAsync(client, path, JsonSerializer.Serialize(new { reason }));
        }

        public static async Task<JsonElement> ReadListAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement list = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(JsonValueKind.Array, list.ValueKind);

            return list;
        }

        public static async Task<JsonElement> ReadPeopleAsync(HttpClient admin, string? query)
        {
            return await ReadListAsync(await PeopleAsync(admin, query));
        }

        public static async Task<JsonElement> ReadLogAsync(HttpClient admin, int? limit = null)
        {
            return await ReadListAsync(await LogAsync(admin, limit));
        }

        public static async Task<JsonElement> ReadGroupAsync(HttpClient admin, Guid groupId)
        {
            HttpResponseMessage response = await GroupAsync(admin, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        public static List<Guid> IdsOf(JsonElement list)
        {
            return [.. list.EnumerateArray().Select(item => item.GetProperty("id").GetGuid())];
        }
    }
}
