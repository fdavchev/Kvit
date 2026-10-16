using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;

namespace Kvit.Api.Tests.Dashboard
{
    public static class DashboardJson
    {
        public const string PaymentType = "payment";
        public const string ClosingType = "closing";

        public static readonly string[] ResponseFieldNames = ExpenseJson.Sorted("totals", "groups", "needsYou", "recentActivity", "plusTarget");

        public static readonly string[] TotalsFieldNames = ExpenseJson.Sorted("owed", "owe");

        public static readonly string[] AmountFieldNames = ExpenseJson.Sorted("currency", "amountMinor");

        public static readonly string[] GroupFieldNames = ExpenseJson.Sorted("id", "kind", "name", "emoji", "status", "balances");

        public static readonly string[] PaymentItemFieldNames = ExpenseJson.Sorted("type", "groupId", "groupName", "settlementId", "fromName", "amountMinor", "currency");

        public static readonly string[] ClosingItemFieldNames = ExpenseJson.Sorted("type", "groupId", "groupName", "startedByName");

        public static readonly string[] ActivityRowFieldNames = ExpenseJson.Sorted([.. ExpenseJson.ActivityFieldNames, "groupId", "groupName"]);

        public static readonly string[] PlusTargetFieldNames = ["groupId"];

        public static List<CurrencyAmount> AmountsOf(JsonElement amounts)
        {
            return [.. amounts.EnumerateArray().Select(amount => new CurrencyAmount(
                amount.GetProperty("currency").GetString()!,
                amount.GetProperty("amountMinor").GetInt64()))];
        }

        public static List<CurrencyAmount> OwedOf(JsonElement body)
        {
            return AmountsOf(body.GetProperty("totals").GetProperty("owed"));
        }

        public static List<CurrencyAmount> OweOf(JsonElement body)
        {
            return AmountsOf(body.GetProperty("totals").GetProperty("owe"));
        }

        public static List<Guid> GroupIdsOf(JsonElement body)
        {
            return GroupRequests.IdsOf(body.GetProperty("groups"));
        }

        public static JsonElement GroupRowOf(JsonElement body, Guid groupId)
        {
            return GroupRequests.RowWithId(body.GetProperty("groups"), groupId);
        }

        public static List<CurrencyAmount> BalancesOfGroup(JsonElement body, Guid groupId)
        {
            return AmountsOf(GroupRowOf(body, groupId).GetProperty("balances"));
        }

        public static List<JsonElement> NeedsYouOfType(JsonElement body, string type)
        {
            return [.. body.GetProperty("needsYou").EnumerateArray().Where(item => item.GetProperty("type").GetString() == type)];
        }

        public static List<JsonElement> PaymentItemsOf(JsonElement body)
        {
            return NeedsYouOfType(body, PaymentType);
        }

        public static List<JsonElement> ClosingItemsOf(JsonElement body)
        {
            return NeedsYouOfType(body, ClosingType);
        }

        public static List<Guid> SettlementIdsOf(JsonElement body)
        {
            return [.. PaymentItemsOf(body).Select(item => item.GetProperty("settlementId").GetGuid()).Order()];
        }

        public static List<Guid> ClosingGroupIdsOf(JsonElement body)
        {
            return [.. ClosingItemsOf(body).Select(item => item.GetProperty("groupId").GetGuid()).Order()];
        }

        public static List<Guid> ActivityGroupIdsOf(JsonElement body)
        {
            return [.. body.GetProperty("recentActivity").EnumerateArray().Select(row => row.GetProperty("groupId").GetGuid())];
        }

        public static List<string> ActivityTypesOf(JsonElement body)
        {
            return ExpenseJson.TypesOf(body.GetProperty("recentActivity"));
        }

        public static List<string> ActivityGroupNamesOf(JsonElement body)
        {
            return [.. body.GetProperty("recentActivity").EnumerateArray().Select(row => row.GetProperty("groupName").GetString()!)];
        }

        public static List<string> ActivityActorNamesOf(JsonElement body)
        {
            return [.. body.GetProperty("recentActivity").EnumerateArray().Select(row => row.GetProperty("actorName").GetString()!)];
        }

        public static Guid? PlusTargetGroupIdOf(JsonElement body)
        {
            JsonElement plusTarget = body.GetProperty("plusTarget");
            if (plusTarget.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return plusTarget.GetProperty("groupId").GetGuid();
        }
    }
}
