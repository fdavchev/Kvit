using Kvit.Contracts.PersonalCategories;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record PersonalEntryRow(
        Guid Id,
        PersonalEntryKind Kind,
        Guid? CategoryId,
        PersonalCategoryRow? Category,
        long AmountMinor,
        Currency Currency,
        DateOnly EntryDate,
        string? Note,
        decimal MkdPerEur,
        DateOnly RateDate,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
