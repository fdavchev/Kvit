using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Dashboard
{
    public sealed record CurrencyAmountRow(Currency Currency, long AmountMinor);
}
