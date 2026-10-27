using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.GroupSpending;
using Kvit.Domain.Entities;
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

            Guid? ownerUserId = await _context.OwnerOfGroupForMember(query.GroupId, userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (ownerUserId is null)
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

            return Result.Ok(new GroupSpendingResponse(currencies, null));
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
