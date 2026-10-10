namespace Kvit.Contracts.Balances
{
    public sealed record MemberBalanceRow(Guid MemberId, string Name, long BalanceMinor, bool IsCurrent);
}
