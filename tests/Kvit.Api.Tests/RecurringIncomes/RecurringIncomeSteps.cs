using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public static class RecurringIncomeSteps
    {
        public const string ListProperty = "recurringIncomes";

        public static async Task<Guid> CreateAsync(HttpClient client, RecurringIncomeBody body, Guid? clientRequestId = null)
        {
            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(client, body, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        public static async Task<Guid> CreateAndStopAsync(HttpClient client, RecurringIncomeBody body)
        {
            Guid recurringIncomeId = await CreateAsync(client, body);
            HttpResponseMessage stopped = await RecurringIncomeRequests.StopAsync(client, recurringIncomeId);

            Assert.Equal(HttpStatusCode.NoContent, stopped.StatusCode);
            return recurringIncomeId;
        }

        public static async Task CreateManyAsync(HttpClient client, int count)
        {
            for (int index = 0; index < count; index++)
            {
                await CreateAsync(client, RecurringIncomeBody.Monthly(title: $"Income {index}"));
            }
        }

        public static async Task<JsonElement> ListBodyAsync(HttpClient client)
        {
            HttpResponseMessage response = await RecurringIncomeRequests.ListAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        public static async Task<List<JsonElement>> ListRowsAsync(HttpClient client)
        {
            JsonElement body = await ListBodyAsync(client);

            return [.. body.GetProperty(ListProperty).EnumerateArray()];
        }

        public static async Task<List<Guid>> ListedIdsAsync(HttpClient client)
        {
            List<JsonElement> rows = await ListRowsAsync(client);

            return [.. rows.Select(row => row.GetProperty("id").GetGuid())];
        }

        public static async Task<GuestMember> JoinAsGuestAsync(GroupsApp app)
        {
            SignedInUser owner = await app.SignUpAsync();
            Guid groupId = await app.CreateGroupAsync(owner);

            return await GuestSteps.JoinWithNewNameAsync(app, groupId, "Gost");
        }
    }
}
