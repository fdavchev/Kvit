using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.RecurringIncomes;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.RecurringIncomes
{
    public sealed class GetRecurringIncomeQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<GetRecurringIncomeQuery, RecurringIncomeRow>
    {
        public async Task<Result<RecurringIncomeRow>> Handle(GetRecurringIncomeQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<RecurringIncomeRow>(userId.Error, userId.ErrorCode);
            }

            RecurringIncomeRow? row = await _context.RecurringIncomesOf(userId.Value)
                .Where(income => income.Id == query.RecurringIncomeId)
                .RecurringIncomeRows()
                .SingleOrDefaultAsync(cancellationToken);
            if (row is null)
            {
                return RecurringIncome.NotFound<RecurringIncomeRow>(query.RecurringIncomeId);
            }

            return Result.Ok(row);
        }
    }
}
