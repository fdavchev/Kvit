using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record ConvertedTotalsResponse(Currency Currency, long SpendingMinor, long IncomeMinor, long GroupSharesMinor, long LeftMinor);
}
