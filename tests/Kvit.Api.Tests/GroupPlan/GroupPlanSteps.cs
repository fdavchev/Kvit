using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.GroupSpending;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public static class GroupPlanSteps
    {
        public const long TwoThousandMkd = 200_000;
        public const long SixHundredEur = 60_000;
        public const string SetEventType = "GroupPlanSet";
        public const string RemovedEventType = "GroupPlanRemoved";

        public static readonly string[] PlanFieldNames = ExpenseJson.Sorted(
            "amountMinor", "currency", "spentMinor", "perPersonMinor", "overMinor", "includedOtherCurrencyMinor");

        public static async Task SetAsync(SignedInUser owner, Guid groupId, long amountMinor, string currency)
        {
            HttpResponseMessage response = await GroupPlanRequests.SetAsync(owner.Client, groupId, amountMinor, currency);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        public static async Task RemoveAsync(SignedInUser owner, Guid groupId)
        {
            HttpResponseMessage response = await GroupPlanRequests.DeleteAsync(owner.Client, groupId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        public static async Task<JsonElement> PlanOfAsync(HttpClient client, Guid groupId)
        {
            JsonElement body = await GroupSpendingRequests.ReadAsync(client, groupId);

            return body.GetProperty("plan");
        }

        public static void AssertPlan(
            JsonElement plan,
            long amountMinor,
            string currency,
            long spentMinor,
            long perPersonMinor,
            long overMinor,
            long includedOtherCurrencyMinor)
        {
            Assert.Equal(JsonValueKind.Object, plan.ValueKind);
            Assert.Equal(PlanFieldNames, GroupSpendingJson.PropertyNamesOf(plan));
            Assert.Equal(amountMinor, plan.GetProperty("amountMinor").GetInt64());
            Assert.Equal(currency, plan.GetProperty("currency").GetString());
            Assert.Equal(spentMinor, plan.GetProperty("spentMinor").GetInt64());
            Assert.Equal(perPersonMinor, plan.GetProperty("perPersonMinor").GetInt64());
            Assert.Equal(overMinor, plan.GetProperty("overMinor").GetInt64());
            Assert.Equal(includedOtherCurrencyMinor, plan.GetProperty("includedOtherCurrencyMinor").GetInt64());
        }

        public static async Task<Guid> AddAtRateAsync(ExpensesApp app, ExpenseGroup group, long amountMinor, string currency, decimal mkdPerEur)
        {
            await BudgetCurrencySteps.UseRateAsync(app, mkdPerEur);

            return await GroupSpendingSteps.AddAsync(app, group, amountMinor, currency);
        }

        public static async Task<JsonElement> EventsAsync(SignedInUser viewer, Guid groupId)
        {
            HttpResponseMessage response = await ExpenseRequests.ActivityAsync(viewer.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("events");
        }

        public static async Task<List<string>> EventTypesAsync(SignedInUser viewer, Guid groupId)
        {
            return ExpenseJson.TypesOf(await EventsAsync(viewer, groupId));
        }

        public static async Task<string> UsageSnapshotAsync(ExpensesApp app, params SignedInUser[] users)
        {
            List<string?> counts = await app.QueryAsync(
                "SELECT count(*)::text FROM usage_events WHERE user_id = ANY (@user_ids)",
                new NpgsqlParameter("user_ids", users.Select(user => user.UserId).ToArray()));

            return Assert.Single(counts)!;
        }
    }
}
