namespace Kvit.Contracts.PersonalEntries
{
    public sealed record PersonalEntryMonthResponse(
        string Month,
        IReadOnlyList<PersonalEntryRow> Entries,
        IReadOnlyList<PersonalCurrencyTotalRow> Totals,
        IReadOnlyList<PersonalIncomeTotalRow> IncomeTotals,
        IReadOnlyList<GroupShareRow> GroupShares,
        ConvertedTotalsResponse? ConvertedTotals,
        LimitsResponse Limits);
}
