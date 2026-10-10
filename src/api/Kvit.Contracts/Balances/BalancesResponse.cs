namespace Kvit.Contracts.Balances
{
    public sealed record BalancesResponse(bool IsEveryoneKvit, IReadOnlyList<CurrencyBalancesResponse> Currencies);
}
