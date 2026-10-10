namespace Kvit.Application.Queries.Balances
{
    public sealed record BalanceMemberDto(Guid MemberId, string Name, bool IsCurrent);
}
