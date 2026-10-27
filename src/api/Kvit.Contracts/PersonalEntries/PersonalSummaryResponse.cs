namespace Kvit.Contracts.PersonalEntries
{
    public sealed record PersonalSummaryResponse(
        string Month,
        string PreviousMonth,
        IReadOnlyList<PersonalSummaryCurrencyRow> Currencies,
        PersonalSummaryConvertedTotalsResponse? ConvertedTotals);
}
