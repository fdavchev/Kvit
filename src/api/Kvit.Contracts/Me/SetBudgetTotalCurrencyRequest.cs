using System.Text.Json.Serialization;

namespace Kvit.Contracts.Me
{
    public sealed record SetBudgetTotalCurrencyRequest([property: JsonRequired] string? BudgetTotalCurrency);
}
