using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.GroupSpending
{
    public sealed record GroupSpendingPlanResponse(Currency Currency, long AmountMinor);
}
