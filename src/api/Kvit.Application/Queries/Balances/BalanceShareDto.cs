namespace Kvit.Application.Queries.Balances
{
    public sealed record BalanceShareDto(Guid MemberId, long InputValue, long ShareMinor);
}
