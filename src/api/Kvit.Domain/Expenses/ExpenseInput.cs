using Kvit.Domain.MoneyRules;

namespace Kvit.Domain.Expenses
{
    public sealed record ExpenseInput(
        string? Title,
        string? Note,
        long AmountMinor,
        string Currency,
        string ExpenseDate,
        Guid? CategoryId,
        Guid PaidByMemberId,
        string SplitType,
        IReadOnlyList<SplitInput> Shares);
}
