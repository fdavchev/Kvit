using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Application.Queries.PersonalEntries
{
    public static class BudgetItems
    {
        public static IEnumerable<SpentItemDto> SpentItemsOf(IEnumerable<PersonalEntryDto> entries)
        {
            return entries
                .Where(entry => entry.Kind == PersonalEntryKind.Spending)
                .Select(entry => new SpentItemDto(entry.CategoryId, entry.AmountMinor, entry.Currency, entry.MkdPerEur));
        }

        public static IEnumerable<SpentItemDto> SpentItemsOf(IEnumerable<GroupShareDto> groupShares)
        {
            return groupShares
                .Select(share => new SpentItemDto(share.Row.CategoryId, share.Row.ShareMinor, share.Row.Currency, share.MkdPerEur));
        }

        public static List<SpentItemDto> SpentItemsOf(IEnumerable<PersonalEntryDto> entries, IEnumerable<GroupShareDto> groupShares)
        {
            return [.. SpentItemsOf(entries).Concat(SpentItemsOf(groupShares))];
        }

        public static long ConvertedSumOf(IEnumerable<SpentItemDto> items, Currency target)
        {
            return items.Sum(item => CurrencyConversion.Convert(item.AmountMinor, item.Currency, target, item.MkdPerEur));
        }

        public static long ConvertedIncomeOf(IEnumerable<PersonalEntryDto> entries, Currency target)
        {
            return entries
                .Where(entry => entry.Kind == PersonalEntryKind.Income)
                .Sum(entry => CurrencyConversion.Convert(entry.AmountMinor, entry.Currency, target, entry.MkdPerEur));
        }
    }
}
