namespace Kvit.Domain.MoneyRules
{
    public sealed record BalanceExpense(Guid PayerMemberId, Money Amount, IReadOnlyList<SplitShare> Shares, bool IsDeleted);
}
