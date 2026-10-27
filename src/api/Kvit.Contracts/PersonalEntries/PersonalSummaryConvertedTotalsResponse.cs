using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record PersonalSummaryConvertedTotalsResponse(
        Currency Currency,
        long SpendingThisMinor,
        long SpendingPreviousMinor,
        long IncomeThisMinor,
        long IncomePreviousMinor);
}
