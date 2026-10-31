using System.Globalization;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Me;
using Kvit.Contracts.PersonalCategories;
using Kvit.Contracts.PersonalEntries;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.PersonalEntries
{
    public sealed class GetPersonalSummaryQueryHandler(
        IDispatcher _dispatcher,
        AppDbContext _context) : IQueryHandler<GetPersonalSummaryQuery, PersonalSummaryResponse>
    {
        public async Task<Result<PersonalSummaryResponse>> Handle(GetPersonalSummaryQuery query, CancellationToken cancellationToken)
        {
            Result<MeResponse> me = await _dispatcher.FullAccountOfCallerAsync(cancellationToken);
            if (!me.IsSuccess)
            {
                return me.ToFailure<PersonalSummaryResponse>();
            }

            Result<DateOnly> firstDay = GetPersonalEntriesQueryHandler.FirstDayOf(query.Month);
            if (!firstDay.IsSuccess)
            {
                return firstDay.ToFailure<PersonalSummaryResponse>();
            }

            DateOnly firstDayOfPreviousMonth = firstDay.Value.AddMonths(-1);
            SummaryMonthDto thisMonth = await SummaryMonthOfAsync(me.Value.Id, firstDay.Value, cancellationToken);
            SummaryMonthDto previousMonth = await SummaryMonthOfAsync(me.Value.Id, firstDayOfPreviousMonth, cancellationToken);
            List<PersonalCategoryRow> categoriesInOrder = await _context.PersonalCategoryRowsOfAsync(
                thisMonth.SpentItems.Concat(previousMonth.SpentItems).Select(item => item.CategoryId),
                cancellationToken);

            return Result.Ok(new PersonalSummaryResponse(
                firstDay.Value.ToString(GetPersonalEntriesQueryHandler.MonthFormat, CultureInfo.InvariantCulture),
                firstDayOfPreviousMonth.ToString(GetPersonalEntriesQueryHandler.MonthFormat, CultureInfo.InvariantCulture),
                [.. CurrenciesOf(thisMonth, previousMonth).Select(currency => CurrencyRowOf(currency, thisMonth, previousMonth, categoriesInOrder))],
                ConvertedTotalsOf(me.Value.BudgetTotalCurrency, thisMonth, previousMonth)));
        }

        private async Task<SummaryMonthDto> SummaryMonthOfAsync(Guid userId, DateOnly firstDay, CancellationToken cancellationToken)
        {
            DateOnly firstDayOfNextMonth = firstDay.AddMonths(1);
            List<PersonalEntryDto> entries = await _context.EntriesOf(userId, firstDay, firstDayOfNextMonth)
                .PersonalEntryDtos(_context, userId)
                .ToListAsync(cancellationToken);
            List<GroupShareDto> groupShares = await _context.GroupSharesOf(userId, firstDay, firstDayOfNextMonth).ToListAsync(cancellationToken);

            return new SummaryMonthDto(entries, BudgetItems.SpentItemsOf(entries, groupShares));
        }

        private static IEnumerable<Currency> CurrenciesOf(SummaryMonthDto thisMonth, SummaryMonthDto previousMonth)
        {
            return thisMonth.Entries.Concat(previousMonth.Entries)
                .Where(entry => entry.Kind == PersonalEntryKind.Income)
                .Select(entry => entry.Currency)
                .Concat(thisMonth.SpentItems.Concat(previousMonth.SpentItems).Select(item => item.Currency))
                .Distinct()
                .Order();
        }

        private static PersonalSummaryCurrencyRow CurrencyRowOf(Currency currency, SummaryMonthDto thisMonth, SummaryMonthDto previousMonth, IReadOnlyList<PersonalCategoryRow> categoriesInOrder)
        {
            Dictionary<Guid, long> thisMonthByCategory = CategorySumsOf(thisMonth.SpentItems, currency);
            Dictionary<Guid, long> previousMonthByCategory = CategorySumsOf(previousMonth.SpentItems, currency);

            return new PersonalSummaryCurrencyRow(
                currency,
                SpendingOf(thisMonth.SpentItems, currency),
                SpendingOf(previousMonth.SpentItems, currency),
                IncomeOf(thisMonth.Entries, currency),
                IncomeOf(previousMonth.Entries, currency),
                [.. categoriesInOrder
                    .Where(category => thisMonthByCategory.ContainsKey(category.Id) || previousMonthByCategory.ContainsKey(category.Id))
                    .Select(category => new PersonalSummaryCategoryRow(
                        category.Id,
                        thisMonthByCategory.GetValueOrDefault(category.Id),
                        previousMonthByCategory.GetValueOrDefault(category.Id)))]);
        }

        private static long SpendingOf(IEnumerable<SpentItemDto> items, Currency currency)
        {
            return items.Where(item => item.Currency == currency).Sum(item => item.AmountMinor);
        }

        private static long IncomeOf(IEnumerable<PersonalEntryDto> entries, Currency currency)
        {
            return entries
                .Where(entry => entry.Kind == PersonalEntryKind.Income && entry.Currency == currency)
                .Sum(entry => entry.AmountMinor);
        }

        private static Dictionary<Guid, long> CategorySumsOf(IEnumerable<SpentItemDto> items, Currency currency)
        {
            return items
                .Where(item => item.Currency == currency && item.CategoryId is not null)
                .GroupBy(item => item.CategoryId!.Value)
                .ToDictionary(categoryItems => categoryItems.Key, categoryItems => categoryItems.Sum(item => item.AmountMinor));
        }

        private static PersonalSummaryConvertedTotalsResponse? ConvertedTotalsOf(Currency? budgetTotalCurrency, SummaryMonthDto thisMonth, SummaryMonthDto previousMonth)
        {
            if (budgetTotalCurrency is not Currency target)
            {
                return null;
            }

            return new PersonalSummaryConvertedTotalsResponse(
                target,
                BudgetItems.ConvertedSumOf(thisMonth.SpentItems, target),
                BudgetItems.ConvertedSumOf(previousMonth.SpentItems, target),
                BudgetItems.ConvertedIncomeOf(thisMonth.Entries, target),
                BudgetItems.ConvertedIncomeOf(previousMonth.Entries, target));
        }
    }
}
