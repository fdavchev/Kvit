namespace Kvit.Domain.Settlements
{
    public sealed record SettlementInput(Guid FromMemberId, Guid ToMemberId, long AmountMinor, string Currency);
}
