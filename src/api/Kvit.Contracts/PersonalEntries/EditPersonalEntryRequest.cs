using System.ComponentModel.DataAnnotations;
using Kvit.Domain.Entities;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record EditPersonalEntryRequest(
        Guid? CategoryId,
        long AmountMinor,
        string Currency,
        string EntryDate,
        string? Note,
        [EnumDataType(typeof(PersonalEntryKind))] PersonalEntryKind Kind = PersonalEntryKind.Spending);
}
