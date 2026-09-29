namespace Kvit.Domain.MoneyRules
{
    public sealed record BalanceSettlement(Guid FromMemberId, Guid ToMemberId, Money Amount, SettlementStatus Status, bool IsDeleted);
}
