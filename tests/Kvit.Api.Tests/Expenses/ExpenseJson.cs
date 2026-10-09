using System.Text.Json;

namespace Kvit.Api.Tests.Expenses
{
    public static class ExpenseJson
    {
        public static readonly string[] DetailFieldNames = Sorted(
            "id", "groupId", "title", "note", "amountMinor", "currency", "expenseDate", "categoryId", "paidByMemberId", "paidByName", "splitType",
            "mkdPerEur", "rateDate", "createdByUserId", "createdByName", "createdAt", "updatedAt", "canEdit", "shares", "history");

        public static readonly string[] ShareFieldNames = Sorted("memberId", "name", "inputValue", "shareMinor");

        public static readonly string[] HistoryFieldNames = Sorted("type", "actorName", "createdAt", "changes");

        public static readonly string[] ListRowFieldNames = Sorted(
            "id", "title", "amountMinor", "currency", "expenseDate", "categoryId", "paidByMemberId", "paidByName", "yourShareMinor", "canEdit");

        public static readonly string[] DeletedRowFieldNames = Sorted(
            "id", "title", "amountMinor", "currency", "expenseDate", "categoryId", "paidByName", "deletedAt", "restorableUntil", "canEdit");

        public static readonly string[] ActivityFieldNames = Sorted(
            "id", "type", "actorUserId", "actorName", "expenseId", "memberId", "changes", "data", "createdAt");

        public static string[] Sorted(params string[] names)
        {
            return [.. names.Order(StringComparer.Ordinal)];
        }

        public static List<long> ShareMinors(JsonElement detail)
        {
            return [.. detail.GetProperty("shares").EnumerateArray().Select(share => share.GetProperty("shareMinor").GetInt64())];
        }

        public static List<long> InputValues(JsonElement detail)
        {
            return [.. detail.GetProperty("shares").EnumerateArray().Select(share => share.GetProperty("inputValue").GetInt64())];
        }

        public static List<Guid> ShareMemberIds(JsonElement detail)
        {
            return [.. detail.GetProperty("shares").EnumerateArray().Select(share => share.GetProperty("memberId").GetGuid())];
        }

        public static List<string> ShareNames(JsonElement detail)
        {
            return [.. detail.GetProperty("shares").EnumerateArray().Select(share => share.GetProperty("name").GetString()!)];
        }

        public static List<Guid> IdsOf(JsonElement expenses)
        {
            return [.. expenses.EnumerateArray().Select(expense => expense.GetProperty("id").GetGuid())];
        }

        public static JsonElement RowWithId(JsonElement expenses, Guid expenseId)
        {
            return expenses.EnumerateArray().Single(expense => expense.GetProperty("id").GetGuid() == expenseId);
        }

        public static List<string> TypesOf(JsonElement events)
        {
            return [.. events.EnumerateArray().Select(entry => entry.GetProperty("type").GetString()!)];
        }
    }
}
