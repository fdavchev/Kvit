namespace Kvit.Api.Tests.PersonalEntries
{
    public sealed record IncomeEntryInput(long AmountMinor, string Currency, string EntryDate, string? Note)
    {
        public const long Salary = 4_500_000;

        public static IncomeEntryInput Of(
            long amountMinor = Salary,
            string currency = PersonalEntryRequests.Mkd,
            string entryDate = PersonalEntryRequests.DefaultDate,
            string? note = null)
        {
            return new IncomeEntryInput(amountMinor, currency, entryDate, note);
        }
    }
}
