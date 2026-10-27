using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record PersonalSummaryCurrencyRow(
        Currency Currency,
        long SpendingThisMinor,
        long SpendingPreviousMinor,
        long IncomeThisMinor,
        long IncomePreviousMinor,
        IReadOnlyList<PersonalSummaryCategoryRow> Categories);
}
