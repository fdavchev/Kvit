using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.GroupSpending
{
    public sealed record CurrencySpendingResponse(Currency Currency, long TotalMinor, IReadOnlyList<CategorySpendingRow> Categories);
}
