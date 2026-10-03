using System.Net.Http.Json;
using System.Text.Json;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public static class GroupRequests
    {
        public const string GroupsPath = "/api/groups";

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
