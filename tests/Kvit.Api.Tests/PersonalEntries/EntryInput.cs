namespace Kvit.Api.Tests.PersonalEntries
{
    public sealed record EntryInput(Guid CategoryId, long AmountMinor, string Currency, string EntryDate, string? Note)
    {
        public static EntryInput Of(
            Guid categoryId,
            long amountMinor = PersonalEntryRequests.OneThousandMkd,
            string currency = PersonalEntryRequests.Mkd,
            string entryDate = PersonalEntryRequests.DefaultDate,
            string? note = null)
        {
            return new EntryInput(categoryId, amountMinor, currency, entryDate, note);
        }
    }
}
