using System.Text.Json;
using Kvit.Api.Tests.Expenses;

namespace Kvit.Api.Tests.GroupSpending
{
    public static class GroupSpendingJson
    {
        public static readonly string[] ResponseFieldNames = ExpenseJson.Sorted("currencies", "plan");

        public static readonly string[] CurrencyFieldNames = ExpenseJson.Sorted("currency", "totalMinor", "categories");

        public static readonly string[] CategoryFieldNames = ExpenseJson.Sorted("categoryId", "totalMinor");

        public static List<string> CurrencyCodesOf(JsonElement body)
        {
            return [.. body.GetProperty("currencies").EnumerateArray().Select(entry => entry.GetProperty("currency").GetString()!)];
        }

        public static JsonElement CurrencyOf(JsonElement body, string currency)
        {
            return body.GetProperty("currencies").EnumerateArray().Single(entry => entry.GetProperty("currency").GetString() == currency);
        }

        public static long TotalOf(JsonElement body, string currency)
        {
            return CurrencyOf(body, currency).GetProperty("totalMinor").GetInt64();
        }

        public static List<CategoryTotal> CategoriesOf(JsonElement body, string currency)
        {
            return [.. CurrencyOf(body, currency).GetProperty("categories").EnumerateArray().Select(CategoryTotalOf)];
        }

        public static string[] PropertyNamesOf(JsonElement element)
        {
            return ExpenseJson.Sorted([.. element.EnumerateObject().Select(property => property.Name)]);
        }

        private static CategoryTotal CategoryTotalOf(JsonElement row)
        {
            JsonElement categoryId = row.GetProperty("categoryId");

            return new CategoryTotal(
                categoryId.ValueKind == JsonValueKind.Null ? null : categoryId.GetGuid(),
                row.GetProperty("totalMinor").GetInt64());
        }
    }
}
