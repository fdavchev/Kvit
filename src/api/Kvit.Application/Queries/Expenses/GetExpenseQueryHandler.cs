using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Activity;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Expenses;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Expenses
{
    public sealed class GetExpenseQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetExpenseQuery, ExpenseDetailResponse>
    {
        public async Task<Result<ExpenseDetailResponse>> Handle(GetExpenseQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<ExpenseDetailResponse>(userId.Error, userId.ErrorCode);
            }

            Guid? ownerUserId = await _context.OwnerOfGroupForMember(query.GroupId, userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (ownerUserId is not Guid owner)
            {
                return Group.NotFound<ExpenseDetailResponse>(query.GroupId);
            }

            ExpenseDetailDto? expense = await ExpenseDetailDtoOf(query.GroupId, query.ExpenseId, userId.Value, owner).SingleOrDefaultAsync(cancellationToken);
            if (expense is null)
            {
                return Expense.NotFound<ExpenseDetailResponse>(query.ExpenseId);
            }

            List<ExpenseShareRow> shares = await ExpenseShareRowsOf(query.GroupId, query.ExpenseId).ToListAsync(cancellationToken);
            List<ExpenseHistoryDto> history = await ExpenseHistoryDtosOf(query.GroupId, query.ExpenseId).ToListAsync(cancellationToken);

            return Result.Ok(new ExpenseDetailResponse(
                expense.Id,
                expense.GroupId,
                expense.Title,
                expense.Note,
                expense.AmountMinor,
                expense.Currency,
                expense.ExpenseDate,
                expense.CategoryId,
                expense.PaidByMemberId,
                expense.PaidByName,
                expense.SplitType,
                expense.MkdPerEur,
                expense.RateDate,
                expense.CreatedByUserId,
                expense.CreatedByName,
                expense.CreatedAt,
                expense.UpdatedAt,
                expense.CanEdit,
                shares,
                [.. history.Select(entry => new ExpenseHistoryRow(entry.Type, entry.ActorName, entry.CreatedAt, ActivityJson.Parse(entry.Changes)))]));
        }

        private IQueryable<ExpenseDetailDto> ExpenseDetailDtoOf(Guid groupId, Guid expenseId, Guid userId, Guid ownerUserId)
        {
            return _context.Expenses
                .AsNoTracking()
                .Where(expense => expense.Id == expenseId && expense.GroupId == groupId && expense.DeletedAt == null)
                .Join(_context.NamedGroupMembers(groupId), expense => expense.PaidByMemberId, payer => payer.Member.Id, (expense, payer) => new { Expense = expense, PaidByName = payer.DisplayName })
                .Join(_context.Users, row => row.Expense.CreatedByUserId, user => user.Id, (row, user) => new ExpenseDetailDto(
                    row.Expense.Id,
                    row.Expense.GroupId,
                    row.Expense.Title,
                    row.Expense.Note,
                    row.Expense.AmountMinor,
                    row.Expense.Currency,
                    row.Expense.ExpenseDate,
                    row.Expense.CategoryId,
                    row.Expense.PaidByMemberId,
                    row.PaidByName,
                    row.Expense.SplitType,
                    row.Expense.MkdPerEur,
                    row.Expense.RateDate,
                    row.Expense.CreatedByUserId,
                    user.DisplayName,
                    row.Expense.CreatedAt,
                    row.Expense.UpdatedAt,
                    row.Expense.CreatedByUserId == userId || ownerUserId == userId));
        }

        private IQueryable<ExpenseShareRow> ExpenseShareRowsOf(Guid groupId, Guid expenseId)
        {
            return _context.ExpenseShares
                .AsNoTracking()
                .Where(share => share.ExpenseId == expenseId)
                .Join(_context.NamedGroupMembers(groupId), share => share.MemberId, named => named.Member.Id, (share, named) => new { Share = share, Named = named })
                .OrderBy(row => row.Named.Member.JoinedAt)
                .ThenBy(row => row.Named.Member.Id)
                .Select(row => new ExpenseShareRow(row.Share.MemberId, row.Named.DisplayName, row.Share.InputValue, row.Share.ShareMinor));
        }

        private IQueryable<ExpenseHistoryDto> ExpenseHistoryDtosOf(Guid groupId, Guid expenseId)
        {
            return _context.ActivityEvents
                .AsNoTracking()
                .Where(activityEvent => activityEvent.GroupId == groupId && activityEvent.ExpenseId == expenseId)
                .Join(_context.Users, activityEvent => activityEvent.ActorUserId, user => user.Id, (activityEvent, user) => new { Event = activityEvent, ActorName = user.DisplayName })
                .OrderByDescending(row => row.Event.CreatedAt)
                .ThenByDescending(row => row.Event.Id)
                .Select(row => new ExpenseHistoryDto(row.Event.Type, row.ActorName, row.Event.CreatedAt, row.Event.Changes));
        }
    }
}
