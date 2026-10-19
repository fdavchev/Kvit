using System.Globalization;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.PersonalCategories;
using Kvit.Contracts.PersonalEntries;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.PersonalEntries
{
    public sealed class GetPersonalEntriesQueryHandler(
        IDispatcher _dispatcher,
        AppDbContext _context) : IQueryHandler<GetPersonalEntriesQuery, PersonalEntryMonthResponse>
    {
        public const string MonthFormat = "yyyy-MM";

        public async Task<Result<PersonalEntryMonthResponse>> Handle(GetPersonalEntriesQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<PersonalEntryMonthResponse>();
            }

            Result<DateOnly> firstDay = FirstDayOf(query.Month);
            if (!firstDay.IsSuccess)
            {
                return firstDay.ToFailure<PersonalEntryMonthResponse>();
            }

            DateOnly firstDayOfNextMonth = firstDay.Value.AddMonths(1);
            List<PersonalEntryDto> entries = await _context.SpendingOf(userId.Value)
                .Where(entry => entry.EntryDate >= firstDay.Value && entry.EntryDate < firstDayOfNextMonth)
                .PersonalEntryDtos()
                .ToListAsync(cancellationToken);
            List<PersonalCategoryRow> categories = await _context.PersonalCategoryRowsOfAsync(entries, cancellationToken);
            Dictionary<Guid, PersonalCategoryRow> categoriesById = categories.ToDictionary(category => category.Id);

            return Result.Ok(new PersonalEntryMonthResponse(
                firstDay.Value.ToString(MonthFormat, CultureInfo.InvariantCulture),
                [.. entries.Select(entry => PersonalEntryQueries.PersonalEntryRowOf(entry, categoriesById))],
                TotalsOf(entries, categories)));
        }

        private static Result<DateOnly> FirstDayOf(string? month)
        {
            if (!DateOnly.TryParseExact(month, MonthFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly firstDay))
            {
                return Result.Failure<DateOnly>($"'{month}' is not a month in the form {MonthFormat}, such as 2026-10.", ResultCodes.PERSONAL_ENTRY_MONTH_INVALID);
            }

            return Result.Ok(firstDay);
        }

        private static List<PersonalCurrencyTotalRow> TotalsOf(IReadOnlyList<PersonalEntryDto> entries, IReadOnlyList<PersonalCategoryRow> categoriesInOrder)
        {
            return [.. entries
                .GroupBy(entry => entry.Currency)
                .OrderBy(currencyEntries => currencyEntries.Key)
                .Select(currencyEntries => new PersonalCurrencyTotalRow(
                    currencyEntries.Key,
                    currencyEntries.Sum(entry => entry.AmountMinor),
                    CategoryTotalsOf(currencyEntries, categoriesInOrder)))];
        }

        private static List<PersonalCategoryTotalRow> CategoryTotalsOf(IEnumerable<PersonalEntryDto> currencyEntries, IReadOnlyList<PersonalCategoryRow> categoriesInOrder)
        {
            Dictionary<Guid, List<PersonalEntryDto>> entriesByCategory = currencyEntries
                .GroupBy(entry => entry.CategoryId)
                .ToDictionary(categoryEntries => categoryEntries.Key, categoryEntries => categoryEntries.ToList());

            return [.. categoriesInOrder
                .Where(category => entriesByCategory.ContainsKey(category.Id))
                .Select(category => new PersonalCategoryTotalRow(
                    category.Id,
                    category,
                    entriesByCategory[category.Id].Sum(entry => entry.AmountMinor),
                    entriesByCategory[category.Id].Count))];
        }
    }
}
