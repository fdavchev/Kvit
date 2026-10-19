namespace Kvit.Contracts.PersonalEntries
{
    public sealed record EditPersonalEntryRequest(
        Guid CategoryId,
        long AmountMinor,
        string Currency,
        string EntryDate,
        string? Note);
}
