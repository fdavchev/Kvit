using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Me
{
    public sealed record ExportedExpense(
        Guid Id,
        string? Title,
        string? Note,
        long AmountMinor,
        Currency Currency,
        DateOnly Date,
        string? Category,
        Guid PaidByMemberId,
        SplitType SplitType,
        DateTimeOffset CreatedAt,
        IReadOnlyList<ExportedShare> Shares);
}
