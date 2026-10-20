namespace Kvit.Domain.PersonalEntries
{
    public sealed record PersonalEntryInput(
        Guid CategoryId,
        long AmountMinor,
        string Currency,
        string EntryDate,
        string? Note);
}
