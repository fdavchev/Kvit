using Kvit.Contracts.Validation;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record AddPersonalEntryRequest(
        [NotEmptyGuid] Guid ClientRequestId,
        Guid CategoryId,
        long AmountMinor,
        string Currency,
        string EntryDate,
        string? Note);
}
