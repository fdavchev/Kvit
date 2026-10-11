using Kvit.Domain.MoneyRules;

namespace Kvit.Application.Queries.Balances
{
    public sealed record BalanceSettlementDto(Guid FromMemberId, Guid ToMemberId, long AmountMinor, Currency Currency, SettlementStatus Status);
}
