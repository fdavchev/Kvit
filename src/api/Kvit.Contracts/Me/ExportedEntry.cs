using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Me
{
    public sealed record ExportedEntry(
        Guid Id,
        PersonalEntryKind Kind,
        long AmountMinor,
        Currency Currency,
        DateOnly Date,
        Guid? CategoryId,
        string? Category,
        string? Note,
        decimal MkdPerEur,
        DateOnly RateDate,
        Guid? RecurringIncomeId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
