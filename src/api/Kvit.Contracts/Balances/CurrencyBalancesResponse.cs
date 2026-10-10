using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Balances
{
    public sealed record CurrencyBalancesResponse(Currency Currency, IReadOnlyList<MemberBalanceRow> Balances, IReadOnlyList<PaymentRow> Payments);
}
