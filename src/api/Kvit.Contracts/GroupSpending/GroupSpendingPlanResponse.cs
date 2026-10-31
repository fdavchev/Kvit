using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.GroupSpending
{
    public sealed record GroupSpendingPlanResponse(
        long AmountMinor,
        Currency Currency,
        long SpentMinor,
        long PerPersonMinor,
        int MemberCount,
        long OverMinor,
        long IncludedOtherCurrencyMinor);
}
