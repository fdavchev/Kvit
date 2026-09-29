namespace Kvit.Domain.MoneyRules
{
    public sealed record MemberBalance(Guid MemberId, Money Balance);
}
