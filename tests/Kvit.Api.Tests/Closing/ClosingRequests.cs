using System.Net.Http.Json;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public static class ClosingRequests
    {
        public static string StartPathOf(Guid groupId)
        {
            return $"{GroupRequests.PathOf(groupId)}/closing";
        }

        public static string ConfirmPathOf(Guid groupId)
        {
            return $"{StartPathOf(groupId)}/confirm";
        }

        public static string ObjectPathOf(Guid groupId)
        {
            return $"{StartPathOf(groupId)}/object";
        }

        public static string UnlockPathOf(Guid groupId)
        {
            return $"{GroupRequests.PathOf(groupId)}/unlock";
        }

        public static Task<HttpResponseMessage> StartAsync(HttpClient client, Guid groupId)
        {
            return client.PostAsync(StartPathOf(groupId), null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, Guid groupId)
        {
            return client.PostAsync(ConfirmPathOf(groupId), null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ObjectAsync(HttpClient client, Guid groupId, string? reason = null)
        {
            return client.PostAsJsonAsync(ObjectPathOf(groupId), new { reason }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ObjectWithJsonAsync(HttpClient client, Guid groupId, string json)
        {
            return GroupRequests.PostJsonTextAsync(client, ObjectPathOf(groupId), json);
        }

        public static Task<HttpResponseMessage> UnlockAsync(HttpClient client, Guid groupId)
        {
            return client.PostAsync(UnlockPathOf(groupId), null, TestContext.Current.CancellationToken);
        }
    }
}
