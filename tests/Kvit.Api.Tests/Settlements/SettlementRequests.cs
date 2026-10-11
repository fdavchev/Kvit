using System.Net.Http.Json;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public static class SettlementRequests
    {
        public const long DefaultAmountMinor = 30_000;

        public static string SettlementsPathOf(Guid groupId)
        {
            return $"/api/groups/{groupId}/settlements";
        }

        public static string SettlementPathOf(Guid groupId, Guid settlementId)
        {
            return $"{SettlementsPathOf(groupId)}/{settlementId}";
        }

        public static Task<HttpResponseMessage> RecordAsync(
            HttpClient client,
            Guid groupId,
            Guid fromMemberId,
            Guid toMemberId,
            long amountMinor,
            string currency,
            Guid? clientRequestId = null)
        {
            return client.PostAsJsonAsync(
                SettlementsPathOf(groupId),
                RecordBody(fromMemberId, toMemberId, amountMinor, currency, clientRequestId ?? Guid.CreateVersion7()),
                TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ListAsync(HttpClient client, Guid groupId)
        {
            return client.GetAsync(SettlementsPathOf(groupId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, Guid groupId, Guid settlementId)
        {
            return client.PostAsync($"{SettlementPathOf(groupId, settlementId)}/confirm", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> RejectAsync(HttpClient client, Guid groupId, Guid settlementId)
        {
            return client.PostAsync($"{SettlementPathOf(groupId, settlementId)}/reject", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> CancelAsync(HttpClient client, Guid groupId, Guid settlementId)
        {
            return client.PostAsync($"{SettlementPathOf(groupId, settlementId)}/cancel", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Guid groupId, Guid settlementId)
        {
            return client.DeleteAsync(SettlementPathOf(groupId, settlementId), TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> RestoreAsync(HttpClient client, Guid groupId, Guid settlementId)
        {
            return client.PostAsync($"{SettlementPathOf(groupId, settlementId)}/restore", null, TestContext.Current.CancellationToken);
        }

        public static object RecordBody(Guid fromMemberId, Guid toMemberId, long amountMinor, string currency, Guid clientRequestId)
        {
            return new
            {
                clientRequestId,
                fromMemberId,
                toMemberId,
                amountMinor,
                currency,
            };
        }
    }
}
