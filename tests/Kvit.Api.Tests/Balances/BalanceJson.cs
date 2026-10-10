using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public static class BalanceJson
    {
        public static readonly string[] ResponseFieldNames = ExpenseJson.Sorted("isEveryoneKvit", "currencies");

        public static readonly string[] CurrencyFieldNames = ExpenseJson.Sorted("currency", "balances", "payments");

        public static readonly string[] BalanceRowFieldNames = ExpenseJson.Sorted("memberId", "name", "balanceMinor", "isCurrent");

        public static readonly string[] PaymentFieldNames = ExpenseJson.Sorted("fromMemberId", "toMemberId", "amountMinor");

        public static List<string> CurrencyCodesOf(JsonElement body)
        {
            return [.. body.GetProperty("currencies").EnumerateArray().Select(entry => entry.GetProperty("currency").GetString()!)];
        }

        public static JsonElement CurrencyOf(JsonElement body, string currency)
        {
            return body.GetProperty("currencies").EnumerateArray().Single(entry => entry.GetProperty("currency").GetString() == currency);
        }

        public static List<Guid> BalanceMemberIds(JsonElement currencyEntry)
        {
            return [.. currencyEntry.GetProperty("balances").EnumerateArray().Select(row => row.GetProperty("memberId").GetGuid())];
        }

        public static List<string> BalanceNames(JsonElement currencyEntry)
        {
            return [.. currencyEntry.GetProperty("balances").EnumerateArray().Select(row => row.GetProperty("name").GetString()!)];
        }

        public static List<long> BalanceMinors(JsonElement currencyEntry)
        {
            return [.. currencyEntry.GetProperty("balances").EnumerateArray().Select(row => row.GetProperty("balanceMinor").GetInt64())];
        }

        public static List<bool> BalanceIsCurrents(JsonElement currencyEntry)
        {
            return [.. currencyEntry.GetProperty("balances").EnumerateArray().Select(row => row.GetProperty("isCurrent").GetBoolean())];
        }

        public static List<ExpectedPayment> PaymentsOf(JsonElement currencyEntry)
        {
            return [.. currencyEntry.GetProperty("payments").EnumerateArray().Select(payment => new ExpectedPayment(
                payment.GetProperty("fromMemberId").GetGuid(),
                payment.GetProperty("toMemberId").GetGuid(),
                payment.GetProperty("amountMinor").GetInt64()))];
        }

        public static void AssertEveryCurrencySumsToZero(JsonElement body)
        {
            foreach (JsonElement currencyEntry in body.GetProperty("currencies").EnumerateArray())
            {
                Assert.Equal(0, BalanceMinors(currencyEntry).Sum());
            }
        }
    }
}
