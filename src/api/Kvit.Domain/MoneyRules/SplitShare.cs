namespace Kvit.Domain.MoneyRules
{
    public sealed record SplitShare(Guid MemberId, long InputValue, Money Share);
}
