using Kvit.Application.Dispatching;
using Kvit.Application.Queries.PersonalEntries;
using Kvit.Contracts.Auth;
using Kvit.Contracts.GroupSpending;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.GroupSpending
{
    public sealed class GetGroupSpendingQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetGroupSpendingQuery, GroupSpendingResponse>
    {
        public async Task<Result<GroupSpendingResponse>> Handle(GetGroupSpendingQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<GroupSpendingResponse>(userId.Error, userId.ErrorCode);
            }

            StoredPlanDto? storedPlan = await _context.GroupForMember(query.GroupId, userId.Value)
                .Select(group => new StoredPlanDto(group.PlanAmountMinor, group.PlanCurrency))
                .SingleOrDefaultAsync(cancellationToken);
            if (storedPlan is null)
            {
                return Group.NotFound<GroupSpendingResponse>(query.GroupId);
            }

            List<CategorySpendingDto> categoryTotals = await CategorySpendingDtosOf(query.GroupId).ToListAsync(cancellationToken);
            List<CurrencySpendingResponse> currencies = [.. categoryTotals
                .GroupBy(categoryTotal => categoryTotal.Currency)
                .OrderBy(currencyTotals => currencyTotals.Key)
                .Select(currencyTotals => new CurrencySpendingResponse(
                    currencyTotals.Key,
                    currencyTotals.Sum(categoryTotal => categoryTotal.TotalMinor),
                    [.. currencyTotals
                        .OrderBy(categoryTotal => categoryTotal.CategorySortOrder is null)
                        .ThenBy(categoryTotal => categoryTotal.CategorySortOrder)
                        .Select(categoryTotal => new CategorySpendingRow(categoryTotal.CategoryId, categoryTotal.TotalMinor))]))];

            return Result.Ok(new GroupSpendingResponse(currencies, await PlanResponseOfAsync(query.GroupId, storedPlan, cancellationToken)));
        }

        private async Task<GroupSpendingPlanResponse?> PlanResponseOfAsync(Guid groupId, StoredPlanDto storedPlan, CancellationToken cancellationToken)
        {
            if (storedPlan.AmountMinor is null && storedPlan.Currency is null)
            {
                return null;
            }

            if (storedPlan.AmountMinor is not long amountMinor || storedPlan.Currency is not Currency currency)
            {
                throw new InvalidOperationException($"Group {groupId} has only half a plan stored (amount {storedPlan.AmountMinor}, currency {storedPlan.Currency}).");
            }

            Money plan = Money.Create(amountMinor, currency).Value;
            List<SpentItemDto> expenses = await _context.CountedExpenses()
                .Where(expense => expense.GroupId == groupId)
                .Select(expense => new SpentItemDto(expense.CategoryId, expense.AmountMinor, expense.Currency, expense.MkdPerEur))
                .ToListAsync(cancellationToken);
            int currentMemberCount = await _context.CurrentGroupMembers().CountAsync(member => member.GroupId == groupId, cancellationToken);
            long spentMinor = BudgetItems.ConvertedSumOf(expenses, currency);
            long includedOtherCurrencyMinor = expenses
                .Where(expense => expense.Currency != currency)
                .Sum(expense => expense.AmountMinor);
            long overMinor = Math.Max(0, checked(spentMinor - amountMinor));

            return new GroupSpendingPlanResponse(
                amountMinor,
                currency,
                spentMinor,
                PerPersonShare.Of(plan, currentMemberCount),
                overMinor,
                includedOtherCurrencyMinor);
        }

        private IQueryable<CategorySpendingDto> CategorySpendingDtosOf(Guid groupId)
        {
            return _context.CountedExpenses()
                .Where(expense => expense.GroupId == groupId)
                .LeftJoin(
                    _context.Categories,
                    expense => expense.CategoryId,
                    category => (Guid?)category.Id,
                    (expense, category) => new
                    {
                        expense.Currency,
                        expense.CategoryId,
                        CategorySortOrder = category == null ? (int?)null : category.SortOrder,
                        expense.AmountMinor,
                    })
                .GroupBy(row => new { row.Currency, row.CategoryId, row.CategorySortOrder })
                .Select(categoryRows => new CategorySpendingDto(
                    categoryRows.Key.Currency,
                    categoryRows.Key.CategoryId,
                    categoryRows.Key.CategorySortOrder,
                    categoryRows.Sum(row => row.AmountMinor)));
        }
    }
}
