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
    public sealed class GetPersonalEntriesQueryHandler(
        IDispatcher _dispatcher,
        AppDbContext _context) : IQueryHandler<GetPersonalEntriesQuery, PersonalEntryMonthResponse>
    {
        public const string MonthFormat = "yyyy-MM";

        private static readonly DateOnly LastMonthWithANextMonth = new(DateOnly.MaxValue.Year, DateOnly.MaxValue.Month - 1, 1);

        public async Task<Result<PersonalEntryMonthResponse>> Handle(GetPersonalEntriesQuery query, CancellationToken cancellationToken)
        {
            Result<MeResponse> me = await _dispatcher.FullAccountOfCallerAsync(cancellationToken);
            if (!me.IsSuccess)
            {
                return me.ToFailure<PersonalEntryMonthResponse>();
            }

            Result<DateOnly> firstDay = FirstDayOf(query.Month);
            if (!firstDay.IsSuccess)
            {
                return firstDay.ToFailure<PersonalEntryMonthResponse>();
            }

            DateOnly firstDayOfNextMonth = firstDay.Value.AddMonths(1);
            List<PersonalEntryDto> entries = await _context.EntriesOf(me.Value.Id)
                .Where(entry => entry.EntryDate >= firstDay.Value && entry.EntryDate < firstDayOfNextMonth)
                .PersonalEntryDtos()
                .ToListAsync(cancellationToken);
            List<PersonalCategoryRow> categories = await _context.PersonalCategoryRowsOfAsync(entries, cancellationToken);
            Dictionary<Guid, PersonalCategoryRow> categoriesById = categories.ToDictionary(category => category.Id);
            List<GroupShareDto> groupShares = await _context.GroupSharesOf(me.Value.Id, firstDay.Value, firstDayOfNextMonth).ToListAsync(cancellationToken);

            return Result.Ok(new PersonalEntryMonthResponse(
                firstDay.Value.ToString(MonthFormat, CultureInfo.InvariantCulture),
                [.. entries.Select(entry => PersonalEntryQueries.PersonalEntryRowOf(entry, categoriesById))],
                SpendingTotalsOf(entries, categories),
                IncomeTotalsOf(entries),
                [.. groupShares.Select(share => share.Row)],
                ConvertedTotalsOf(me.Value.BudgetTotalCurrency, entries, groupShares)));
        }

        private static Result<DateOnly> FirstDayOf(string? month)
        {
            if (!DateOnly.TryParseExact(month, MonthFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly firstDay))
            {
                return Result.Failure<DateOnly>($"'{month}' is not a month in the form {MonthFormat}, such as 2026-10.", ResultCodes.PERSONAL_ENTRY_MONTH_INVALID);
            }

            if (firstDay < ExpenseFields.EarliestDate || firstDay > LastMonthWithANextMonth)
            {
                return Result.Failure<DateOnly>($"The month {month} is outside the supported range {ExpenseFields.EarliestDate.ToString(MonthFormat, CultureInfo.InvariantCulture)} to {LastMonthWithANextMonth.ToString(MonthFormat, CultureInfo.InvariantCulture)}.", ResultCodes.PERSONAL_ENTRY_MONTH_INVALID);
            }

            return Result.Ok(firstDay);
        }

        private static List<PersonalCurrencyTotalRow> SpendingTotalsOf(IReadOnlyList<PersonalEntryDto> entries, IReadOnlyList<PersonalCategoryRow> categoriesInOrder)
        {
            return [.. entries
                .Where(entry => entry.Kind == PersonalEntryKind.Spending)
                .GroupBy(entry => entry.Currency)
                .OrderBy(currencyEntries => currencyEntries.Key)
                .Select(currencyEntries => new PersonalCurrencyTotalRow(
                    currencyEntries.Key,
                    currencyEntries.Sum(entry => entry.AmountMinor),
                    CategoryTotalsOf(currencyEntries, categoriesInOrder)))];
        }

        private static List<PersonalIncomeTotalRow> IncomeTotalsOf(IReadOnlyList<PersonalEntryDto> entries)
        {
            return [.. entries
                .Where(entry => entry.Kind == PersonalEntryKind.Income)
                .GroupBy(entry => entry.Currency)
                .OrderBy(currencyEntries => currencyEntries.Key)
                .Select(currencyEntries => new PersonalIncomeTotalRow(
                    currencyEntries.Key,
                    currencyEntries.Sum(entry => entry.AmountMinor),
                    currencyEntries.Count()))];
        }

        private static ConvertedTotalsResponse? ConvertedTotalsOf(Currency? budgetTotalCurrency, IReadOnlyList<PersonalEntryDto> entries, IReadOnlyList<GroupShareDto> groupShares)
        {
            if (budgetTotalCurrency is not Currency target)
            {
                return null;
            }

            long spendingMinor = ConvertedSumOf(entries, PersonalEntryKind.Spending, target);
            long incomeMinor = ConvertedSumOf(entries, PersonalEntryKind.Income, target);
            long groupSharesMinor = groupShares.Sum(share => CurrencyConversion.Convert(share.Row.ShareMinor, share.Row.Currency, target, share.MkdPerEur));

            return new ConvertedTotalsResponse(target, spendingMinor, incomeMinor, groupSharesMinor, checked(incomeMinor - spendingMinor - groupSharesMinor));
        }

        private static long ConvertedSumOf(IEnumerable<PersonalEntryDto> entries, PersonalEntryKind kind, Currency target)
        {
            return entries
                .Where(entry => entry.Kind == kind)
                .Sum(entry => CurrencyConversion.Convert(entry.AmountMinor, entry.Currency, target, entry.MkdPerEur));
        }

        private static List<PersonalCategoryTotalRow> CategoryTotalsOf(IEnumerable<PersonalEntryDto> currencyEntries, IReadOnlyList<PersonalCategoryRow> categoriesInOrder)
        {
            Dictionary<Guid, List<PersonalEntryDto>> entriesByCategory = currencyEntries
                .GroupBy(entry => entry.CategoryId ?? throw new InvalidOperationException($"Spending entry {entry.Id} has no category."))
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
