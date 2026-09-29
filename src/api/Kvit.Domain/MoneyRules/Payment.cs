namespace Kvit.Domain.MoneyRules
{
    public sealed record Payment(Guid FromMemberId, Guid ToMemberId, Money Amount);
}
