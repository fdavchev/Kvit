using System.Text.Json;
using Kvit.Api.Tests.Expenses;

namespace Kvit.Api.Tests.Settlements
{
    public static class SettlementJson
    {
        public static readonly string[] FieldNames = ExpenseJson.Sorted(
            "id", "fromMemberId", "fromName", "toMemberId", "toName", "amountMinor", "currency", "status", "recordedByUserId", "createdAt", "resolvedAt");

        public static readonly string[] ListFieldNames = ExpenseJson.Sorted("settlements");

        public static readonly string[] EventDataFieldNames = ExpenseJson.Sorted("amountMinor", "currency", "fromMemberId", "toMemberId", "fromName", "toName");

        public static List<Guid> IdsOf(JsonElement settlements)
        {
            return [.. settlements.EnumerateArray().Select(settlement => settlement.GetProperty("id").GetGuid())];
        }

        public static List<string> StatusesOf(JsonElement settlements)
        {
            return [.. settlements.EnumerateArray().Select(settlement => settlement.GetProperty("status").GetString()!)];
        }

        public static JsonElement RowWithId(JsonElement settlements, Guid settlementId)
        {
            return settlements.EnumerateArray().Single(settlement => settlement.GetProperty("id").GetGuid() == settlementId);
        }
    }
}
