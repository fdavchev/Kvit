namespace Kvit.Contracts.Settlements
{
    public sealed record SettlementListResponse(IReadOnlyList<SettlementRow> Settlements);
}
