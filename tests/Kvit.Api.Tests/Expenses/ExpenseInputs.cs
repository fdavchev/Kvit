namespace Kvit.Api.Tests.Expenses
{
    public static class ExpenseInputs
    {
        public const string Mkd = "MKD";
        public const string Eur = "EUR";
        public const string DefaultDate = "2026-10-05";
        public const long OneThousandMkd = 100_000;
        public const long TwelveHundredMkd = 120_000;

        public static ExpenseInput Equal(long amountMinor, string currency, Guid payerRowId, params Guid[] memberRowIds)
        {
            return Of("Equal", amountMinor, currency, payerRowId, [.. memberRowIds.Select(memberRowId => new ShareInput(memberRowId, 0))]);
        }

        public static ExpenseInput Of(string splitType, long amountMinor, string currency, Guid payerRowId, params ShareInput[] shares)
        {
            return new ExpenseInput(null, null, amountMinor, currency, DefaultDate, null, payerRowId, splitType, shares);
        }

        public static string TextOfLength(int length)
        {
            return new string('x', length);
        }
    }
}
