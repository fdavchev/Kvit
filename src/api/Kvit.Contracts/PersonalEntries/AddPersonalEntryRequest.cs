using System.ComponentModel.DataAnnotations;
using Kvit.Contracts.Validation;
using Kvit.Domain.Entities;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record AddPersonalEntryRequest(
        [NotEmptyGuid] Guid ClientRequestId,
        Guid? CategoryId,
        long AmountMinor,
        string Currency,
        string EntryDate,
        string? Note,
        [EnumDataType(typeof(PersonalEntryKind))] PersonalEntryKind Kind = PersonalEntryKind.Spending);
}
