using Kvit.Domain.MoneyRules;

namespace Kvit.Application.Queries.Balances
{
    public sealed record BalanceExpenseDto(Guid PaidByMemberId, long AmountMinor, Currency Currency, IReadOnlyList<BalanceShareDto> Shares);
}
