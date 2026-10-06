using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public static class GroupRequests
    {
        public const string GroupsPath = "/api/groups";
        public const string PreviewPath = "/api/invites/preview";
        public const string JoinPath = "/api/invites/join";

        public static readonly Regex TokenFormat = new("^[A-Za-z0-9_-]{43}$");
        public static readonly string[] MemberFieldNames = ["claimedName", "displayName", "id", "isNameOnly", "isOwner", "isYou", "name", "pictureUrl", "userId"];

        public static string PathOf(Guid groupId)
        {
            return $"{GroupsPath}/{groupId}";
        }

        public static Task<HttpResponseMessage> CreateAsync(
            HttpClient client,
            string name = GroupRows.DefaultName,
            string emoji = GroupRows.DefaultEmoji,
            string currency = GroupRows.DefaultCurrency)
        {
            return client.PostAsJsonAsync(GroupsPath, new { name, emoji, currency }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ListAsync(HttpClient client)
        {
            return client.GetAsync(GroupsPath, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> GetAsync(HttpClient client, Guid groupId)
        {
            return client.GetAsync(PathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> UpdateAsync(
            HttpClient client,
            Guid groupId,
            string name = GroupRows.DefaultName,
            string emoji = GroupRows.DefaultEmoji,
            string currency = GroupRows.DefaultCurrency)
        {
            return client.PutAsJsonAsync(PathOf(groupId), new { name, emoji, currency }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Guid groupId)
        {
            return client.DeleteAsync(PathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> RestoreAsync(HttpClient client, Guid groupId)
        {
            return client.PostAsync($"{PathOf(groupId)}/restore", null, TestContext.Current.CancellationToken);
        }

        public static string MembersPathOf(Guid groupId)
        {
            return $"{PathOf(groupId)}/members";
        }

        public static string MemberPathOf(Guid groupId, Guid memberId)
        {
            return $"{MembersPathOf(groupId)}/{memberId}";
        }

        public static Task<HttpResponseMessage> ListMembersAsync(HttpClient client, Guid groupId)
        {
            return client.GetAsync(MembersPathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> AddMemberAsync(HttpClient client, Guid groupId, string name)
        {
            return client.PostAsJsonAsync(MembersPathOf(groupId), new { name }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> RemoveMemberAsync(HttpClient client, Guid groupId, Guid memberId)
        {
            return client.DeleteAsync(MemberPathOf(groupId, memberId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> LetBackInAsync(HttpClient client, Guid groupId, Guid memberId)
        {
            return client.PostAsync($"{MemberPathOf(groupId, memberId)}/let-back-in", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> LeaveAsync(HttpClient client, Guid groupId)
        {
            return client.PostAsync($"{PathOf(groupId)}/leave", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> MakeOwnerAsync(HttpClient client, Guid groupId, Guid memberId)
        {
            return client.PostAsJsonAsync($"{PathOf(groupId)}/owner", new { memberId }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ClaimAsync(HttpClient client, Guid groupId, Guid memberId)
        {
            return client.PostAsync($"{MemberPathOf(groupId, memberId)}/claim", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> UndoClaimAsync(HttpClient client, Guid groupId, Guid memberId)
        {
            return client.PostAsync($"{MemberPathOf(groupId, memberId)}/undo-claim", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ResetInviteAsync(HttpClient client, Guid groupId)
        {
            return client.PostAsync($"{PathOf(groupId)}/invite/reset", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> UndoResetInviteAsync(HttpClient client, Guid groupId)
        {
            return client.PostAsync($"{PathOf(groupId)}/invite/undo-reset", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> PreviewAsync(HttpClient client, string token)
        {
            return client.PostAsJsonAsync(PreviewPath, new { token }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> JoinAsync(HttpClient client, string token, Guid? claimMemberId = null)
        {
            return client.PostAsJsonAsync(JoinPath, new { token, claimMemberId }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> PostJsonTextAsync(HttpClient client, string path, string json)
        {
            StringContent content = new(json, Encoding.UTF8, "application/json");

            return client.PostAsync(path, content, TestContext.Current.CancellationToken);
        }

        public static async Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string path, object body)
        {
            using HttpRequestMessage request = new(new HttpMethod(method), path)
            {
                Content = JsonContent.Create(body),
            };

            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        public static async Task<JsonElement> ReadMembersAsync(HttpClient client, Guid groupId)
        {
            HttpResponseMessage response = await ListMembersAsync(client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        public static async Task<JsonElement> MemberRowAsync(HttpClient client, Guid groupId, Guid memberId)
        {
            JsonElement body = await ReadMembersAsync(client, groupId);

            return RowWithId(body.GetProperty("members"), memberId);
        }

        public static async Task<List<Guid>> MemberIdsAsync(HttpClient client, Guid groupId)
        {
            JsonElement body = await ReadMembersAsync(client, groupId);

            return IdsOf(body.GetProperty("members"));
        }

        public static string[] PropertyNamesOf(JsonElement element)
        {
            return [.. element.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
        }

        public static string[] StringsOf(JsonElement array)
        {
            return [.. array.EnumerateArray().Select(item => item.GetString()!).Order(StringComparer.Ordinal)];
        }

        public static async Task<int> MemberCountOfAsync(HttpClient client, Guid groupId)
        {
            HttpResponseMessage response = await GetAsync(client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("memberCount").GetInt32();
        }

        public static List<Guid> IdsOf(JsonElement groups)
        {
            return [.. groups.EnumerateArray().Select(group => group.GetProperty("id").GetGuid())];
        }

        public static JsonElement RowWithId(JsonElement groups, Guid groupId)
        {
            return groups.EnumerateArray().Single(group => group.GetProperty("id").GetGuid() == groupId);
        }
    }
}
