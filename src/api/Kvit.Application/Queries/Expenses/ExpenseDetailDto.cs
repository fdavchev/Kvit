using Kvit.Domain.MoneyRules;

namespace Kvit.Application.Queries.Expenses
{
    public sealed record ExpenseDetailDto(
        Guid Id,
        Guid GroupId,
        string? Title,
        string? Note,
        long AmountMinor,
        Currency Currency,
        DateOnly ExpenseDate,
        Guid? CategoryId,
        Guid PaidByMemberId,
        string PaidByName,
        SplitType SplitType,
        decimal MkdPerEur,
        DateOnly RateDate,
        Guid CreatedByUserId,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        bool CanEdit);
}
