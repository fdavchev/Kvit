namespace Kvit.Api.Tests.Balances
{
    public sealed record ExpectedPayment(Guid FromMemberId, Guid ToMemberId, long AmountMinor);
}
