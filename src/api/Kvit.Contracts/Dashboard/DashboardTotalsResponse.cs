namespace Kvit.Contracts.Dashboard
{
    public sealed record DashboardTotalsResponse(IReadOnlyList<CurrencyAmountRow> Owed, IReadOnlyList<CurrencyAmountRow> Owe);
}
