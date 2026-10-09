using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Expenses;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Expenses
{
    public sealed class GetExpensesQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetExpensesQuery, ExpenseListResponse>
    {
        public async Task<Result<ExpenseListResponse>> Handle(GetExpensesQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<ExpenseListResponse>(userId.Error, userId.ErrorCode);
            }

            Guid? ownerUserId = await _context.OwnerOfGroupForMember(query.GroupId, userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (ownerUserId is not Guid owner)
            {
                return Group.NotFound<ExpenseListResponse>(query.GroupId);
            }

            Guid ownRowId = await _context.CurrentGroupMembers()
                .AsNoTracking()
                .Where(member => member.GroupId == query.GroupId && member.UserId == userId.Value)
                .Select(member => member.Id)
                .SingleAsync(cancellationToken);
            List<ExpenseListRow> expenses = await ExpenseListRowsOf(query.GroupId, userId.Value, owner, ownRowId).ToListAsync(cancellationToken);

            return Result.Ok(new ExpenseListResponse(expenses));
        }

        private IQueryable<ExpenseListRow> ExpenseListRowsOf(Guid groupId, Guid userId, Guid ownerUserId, Guid ownRowId)
        {
            IQueryable<ExpenseShare> ownShares = _context.ExpenseShares.Where(share => share.MemberId == ownRowId);

            return _context.Expenses
                .AsNoTracking()
                .Where(expense => expense.GroupId == groupId && expense.DeletedAt == null)
                .Join(_context.NamedGroupMembers(groupId), expense => expense.PaidByMemberId, payer => payer.Member.Id, (expense, payer) => new { Expense = expense, PaidByName = payer.DisplayName })
                .OrderByDescending(row => row.Expense.ExpenseDate)
                .ThenByDescending(row => row.Expense.CreatedAt)
                .ThenByDescending(row => row.Expense.Id)
                .Select(row => new ExpenseListRow(
                    row.Expense.Id,
                    row.Expense.Title,
                    row.Expense.AmountMinor,
                    row.Expense.Currency,
                    row.Expense.ExpenseDate,
                    row.Expense.CategoryId,
                    row.Expense.PaidByMemberId,
                    row.PaidByName,
                    ownShares.Where(share => share.ExpenseId == row.Expense.Id).Select(share => (long?)share.ShareMinor).SingleOrDefault(),
                    row.Expense.CreatedByUserId == userId || ownerUserId == userId));
        }
    }
}
