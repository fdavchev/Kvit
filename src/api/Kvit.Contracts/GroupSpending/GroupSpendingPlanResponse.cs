using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.GroupSpending
{
    public sealed record GroupSpendingPlanResponse(
        long AmountMinor,
        Currency Currency,
        long SpentMinor,
        long PerPersonMinor,
        long OverMinor,
        long IncludedOtherCurrencyMinor);
}
