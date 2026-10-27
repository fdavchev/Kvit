namespace Kvit.Application.Queries.PersonalEntries
{
    public sealed record SummaryMonthDto(IReadOnlyList<PersonalEntryDto> Entries, IReadOnlyList<SpentItemDto> SpentItems);
}
