namespace Kvit.Contracts.Balances
{
    public sealed record PaymentRow(Guid FromMemberId, Guid ToMemberId, long AmountMinor);
}
