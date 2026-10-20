using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Application.Queries.PersonalEntries
{
    public sealed record PersonalEntryDto(
        Guid Id,
        PersonalEntryKind Kind,
        Guid? CategoryId,
        long AmountMinor,
        Currency Currency,
        DateOnly EntryDate,
        string? Note,
        decimal MkdPerEur,
        DateOnly RateDate,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
