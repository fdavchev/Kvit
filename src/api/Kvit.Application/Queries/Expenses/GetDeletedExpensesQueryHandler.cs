using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Expenses;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Expenses
{
    public sealed class GetDeletedExpensesQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context,
        TimeProvider _timeProvider) : IQueryHandler<GetDeletedExpensesQuery, DeletedExpenseListResponse>
    {
        public async Task<Result<DeletedExpenseListResponse>> Handle(GetDeletedExpensesQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<DeletedExpenseListResponse>(userId.Error, userId.ErrorCode);
            }

            Guid? ownerUserId = await _context.OwnerOfGroupForMember(query.GroupId, userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (ownerUserId is not Guid owner)
            {
                return Group.NotFound<DeletedExpenseListResponse>(query.GroupId);
            }

            List<DeletedExpenseListRow> expenses = await DeletedExpenseListRowsOf(query.GroupId, userId.Value, owner).ToListAsync(cancellationToken);

            return Result.Ok(new DeletedExpenseListResponse(expenses));
        }

        private IQueryable<DeletedExpenseListRow> DeletedExpenseListRowsOf(Guid groupId, Guid userId, Guid ownerUserId)
        {
            DateTimeOffset oldestRestorableDeletion = _timeProvider.GetUtcNow().AddDays(-Expense.DeletedExpenseRestoreDays);

            return _context.Expenses
                .AsNoTracking()
                .Where(expense => expense.GroupId == groupId && expense.DeletedAt > oldestRestorableDeletion)
                .Join(_context.NamedGroupMembers(groupId), expense => expense.PaidByMemberId, payer => payer.Member.Id, (expense, payer) => new { Expense = expense, PaidByName = payer.DisplayName })
                .OrderByDescending(row => row.Expense.DeletedAt)
                .ThenByDescending(row => row.Expense.Id)
                .Select(row => new DeletedExpenseListRow(
                    row.Expense.Id,
                    row.Expense.Title,
                    row.Expense.AmountMinor,
                    row.Expense.Currency,
                    row.Expense.ExpenseDate,
                    row.Expense.CategoryId,
                    row.PaidByName,
                    row.Expense.DeletedAt!.Value,
                    Expense.RestorableUntil(row.Expense.DeletedAt.Value),
                    row.Expense.CreatedByUserId == userId || ownerUserId == userId));
        }
    }
}
