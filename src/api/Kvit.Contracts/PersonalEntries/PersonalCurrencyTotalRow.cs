using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record PersonalCurrencyTotalRow(Currency Currency, long TotalMinor, IReadOnlyList<PersonalCategoryTotalRow> Categories);
}
