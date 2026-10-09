using Kvit.Domain.MoneyRules;

namespace Kvit.Domain.Expenses
{
    public sealed record ExpenseState(
        long AmountMinor,
        Currency Currency,
        string? Title,
        string? Note,
        DateOnly ExpenseDate,
        string CategoryKey,
        Guid PayerMemberId,
        string PayerName,
        SplitType SplitType,
        IReadOnlyList<SplitLine> SplitInJoiningOrder);
}
