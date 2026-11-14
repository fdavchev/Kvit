using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Settlements;
using Kvit.Contracts.Auth;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public static class DeletionSteps
    {
        public static async Task RequestAsync(HttpClient client, string nameChoice)
        {
            HttpResponseMessage response = await DeletionRequests.DeleteAsync(client, nameChoice);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        public static async Task<DateTimeOffset> RequestAndPurgeAsync(ExpensesApp app, HttpClient client, Guid userId, string nameChoice)
        {
            DateTimeOffset requestedAt = app.Clock.GetUtcNow();
            await RequestAsync(client, nameChoice);
            DateTimeOffset purgedAt = requestedAt + DeletionRequests.GraceTime;

            await AccountPurges.PurgeAsync(app, userId, nameChoice, purgedAt);

            return purgedAt;
        }

        public static async Task<GoogleAccount> SignUpWithGoogleAsync(ExpensesApp app)
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = await app.CreateGoogleSignedUpClientAsync(identity);
            Guid userId = (await app.FindUserAsync(identity.Email)).Id;

            return new GoogleAccount(identity, client, userId);
        }

        public static async Task<List<string>> BalancesAsync(HttpClient viewer, Guid groupId)
        {
            JsonElement body = await BalanceRequests.ReadBalancedAsync(viewer, groupId);
            List<string> lines = [];
            foreach (JsonElement currencyEntry in body.GetProperty("currencies").EnumerateArray())
            {
                string currency = currencyEntry.GetProperty("currency").GetString()!;
                foreach (JsonElement row in currencyEntry.GetProperty("balances").EnumerateArray())
                {
                    lines.Add($"{currency}|{row.GetProperty("memberId").GetGuid()}|{row.GetProperty("balanceMinor").GetInt64()}");
                }
            }

            lines.Sort(StringComparer.Ordinal);
            return lines;
        }

        public static async Task<JsonElement> ReadLogInAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await AuthRequests.ReadJsonAsync(response);
        }

        public static bool DeletionCancelledOf(JsonElement me)
        {
            Assert.True(me.TryGetProperty("deletionCancelled", out JsonElement value), $"The answer has no 'deletionCancelled' property: {me}");

            return value.GetBoolean();
        }
    }
}
