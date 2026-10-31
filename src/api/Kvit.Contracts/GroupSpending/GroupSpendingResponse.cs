namespace Kvit.Contracts.GroupSpending
{
    public sealed record GroupSpendingResponse(IReadOnlyList<CurrencySpendingResponse> Currencies, GroupSpendingPlanResponse? Plan);
}
