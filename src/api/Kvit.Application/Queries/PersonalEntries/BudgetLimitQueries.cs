using Kvit.Contracts.PersonalEntries;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.PersonalEntries
{
    public static class BudgetLimitQueries
    {
        public static async Task<LimitsResponse> LimitsResponseOfAsync(
            this AppDbContext context,
            Guid userId,
            DateOnly firstDayOfMonth,
            IReadOnlyList<PersonalEntryDto> entries,
            IReadOnlyList<GroupShareDto> groupShares,
            CancellationToken cancellationToken)
        {
            List<BudgetLimitDto> rows = await context.BudgetLimits
                .AsNoTracking()
                .Where(limit => limit.UserId == userId && limit.EffectiveFrom <= firstDayOfMonth)
                .Select(limit => new BudgetLimitDto(limit.CategoryId, limit.EffectiveFrom, limit.LimitMinor))
                .ToListAsync(cancellationToken);
            List<BudgetLimitDto> limitsInForce = [.. rows
                .GroupBy(row => row.CategoryId)
                .Select(categoryRows => categoryRows.OrderByDescending(row => row.EffectiveFrom).First())
                .Where(row => row.LimitMinor is not null)];
            List<SpentItemDto> spending = SpentItemsOf(entries, groupShares);

            BudgetLimitDto? monthLimit = limitsInForce.SingleOrDefault(row => row.CategoryId is null);
            LimitRow? monthRow = monthLimit is null ? null : LimitRowOf(monthLimit, spending);

            Dictionary<Guid, BudgetLimitDto> categoryLimits = limitsInForce
                .Where(row => row.CategoryId is not null)
                .ToDictionary(row => row.CategoryId!.Value);
            List<Guid> categoryIds = [.. categoryLimits.Keys];
            List<Guid> categoryIdsInOrder = await context.Categories
                .AsNoTracking()
                .Where(category => categoryIds.Contains(category.Id))
                .OrderBy(category => category.OwnerUserId != null)
                .ThenBy(category => category.SortOrder)
                .ThenBy(category => category.Id)
                .Select(category => category.Id)
                .ToListAsync(cancellationToken);

            return new LimitsResponse(
                monthRow,
                [.. categoryIdsInOrder.Select(categoryId => LimitRowOf(categoryLimits[categoryId], [.. spending.Where(item => item.CategoryId == categoryId)]))]);
        }

        private static List<SpentItemDto> SpentItemsOf(IReadOnlyList<PersonalEntryDto> entries, IReadOnlyList<GroupShareDto> groupShares)
        {
            IEnumerable<SpentItemDto> personal = entries
                .Where(entry => entry.Kind == PersonalEntryKind.Spending)
                .Select(entry => new SpentItemDto(entry.CategoryId, entry.AmountMinor, entry.Currency, entry.MkdPerEur));
            IEnumerable<SpentItemDto> shares = groupShares
                .Select(share => new SpentItemDto(share.Row.CategoryId, share.Row.ShareMinor, share.Row.Currency, share.MkdPerEur));

            return [.. personal.Concat(shares)];
        }

        private static LimitRow LimitRowOf(BudgetLimitDto limit, IReadOnlyList<SpentItemDto> items)
        {
            long limitMinor = limit.LimitMinor ?? throw new InvalidOperationException($"The limit row from {limit.EffectiveFrom} for category {limit.CategoryId} has no amount, so it is not a limit in force.");
            long spentMinor = items.Sum(item => CurrencyConversion.Convert(item.AmountMinor, item.Currency, BudgetLimit.LimitCurrency, item.MkdPerEur));
            long includedEurMinor = items.Where(item => item.Currency == Currency.EUR).Sum(item => item.AmountMinor);

            return new LimitRow(limit.CategoryId, limitMinor, spentMinor, Math.Max(0, spentMinor - limitMinor), includedEurMinor);
        }
    }
}
