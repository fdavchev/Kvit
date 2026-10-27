using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public static class PossibleDuplicateSteps
    {
        public const string Month = PersonalEntryRequests.DefaultMonth;
        public const string MatchedOnShare = "Share";
        public const string MatchedOnWhole = "Whole";
        public const long ShareMinor = BudgetShareSteps.HalfOfOneThousandMkd;
        public const long WholeMinor = ExpenseInputs.OneThousandMkd;

        public static readonly Guid Food = PersonalEntryRequests.BuiltInFoodId;
        public static readonly Guid Transport = PersonalEntryRequests.BuiltInTransportId;

        public static readonly string[] PairFieldNames =
        [
            "currency",
            "entryAmountMinor",
            "entryCategoryId",
            "entryDate",
            "entryId",
            "entryNote",
            "expenseAmountMinor",
            "expenseDate",
            "expenseId",
            "expenseTitle",
            "groupId",
            "groupName",
            "matchedOn",
            "shareMinor",
        ];

        public static Task<Guid> AddEntryAsync(
            SignedInUser user,
            long amountMinor = ShareMinor,
            string entryDate = ExpenseInputs.DefaultDate,
            Guid? categoryId = null,
            string currency = PersonalEntryRequests.Mkd,
            string? note = null)
        {
            return PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(categoryId ?? Food, amountMinor, currency, entryDate, note));
        }

        public static async Task<JsonElement> PairsAsync(HttpClient client, string month = Month)
        {
            JsonElement body = await PersonalEntrySteps.MonthAsync(client, month);

            return body.GetProperty("possibleDuplicates");
        }

        public static async Task<List<(Guid EntryId, Guid ExpenseId)>> PairKeysAsync(HttpClient client, string month = Month)
        {
            JsonElement pairs = await PairsAsync(client, month);

            return [.. pairs.EnumerateArray().Select(pair => (pair.GetProperty("entryId").GetGuid(), pair.GetProperty("expenseId").GetGuid())).Order()];
        }

        public static List<(Guid EntryId, Guid ExpenseId)> Keys(params (Guid EntryId, Guid ExpenseId)[] pairs)
        {
            return [.. pairs.Order()];
        }

        public static async Task<JsonElement> PairAsync(HttpClient client, Guid entryId, Guid expenseId, string month = Month)
        {
            JsonElement pairs = await PairsAsync(client, month);

            return Assert.Single(pairs.EnumerateArray(), pair => pair.GetProperty("entryId").GetGuid() == entryId && pair.GetProperty("expenseId").GetGuid() == expenseId);
        }

        public static async Task AnswerAsync(SignedInUser user, Guid entryId, Guid expenseId, string answer)
        {
            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(user.Client, entryId, expenseId, answer);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public static async Task<Guid?> MergedWithAsync(HttpClient client, Guid entryId, string month = Month)
        {
            JsonElement body = await PersonalEntrySteps.MonthAsync(client, month);
            JsonElement entry = Assert.Single(body.GetProperty("entries").EnumerateArray(), row => row.GetProperty("id").GetGuid() == entryId);
            JsonElement merged = entry.GetProperty("mergedWithExpenseId");

            return merged.ValueKind == JsonValueKind.Null ? null : merged.GetGuid();
        }

        public static async Task<long> MonthSpendingTotalAsync(HttpClient client, string currency = PersonalEntryRequests.Mkd, string month = Month)
        {
            JsonElement body = await PersonalEntrySteps.MonthAsync(client, month);
            JsonElement[] rows = [.. body.GetProperty("totals").EnumerateArray().Where(row => row.GetProperty("currency").GetString() == currency)];

            return rows.Length == 0 ? 0 : rows.Single().GetProperty("totalMinor").GetInt64();
        }
    }
}
