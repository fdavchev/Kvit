namespace Kvit.Domain.PersonalEntries
{
    public sealed record IncomeInput(
        long AmountMinor,
        string Currency,
        string EntryDate,
        string? Note);
}
