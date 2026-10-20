using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.RecurringIncomes;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.RecurringIncomes
{
    public sealed class GetRecurringIncomesQueryHandler(
        IDispatcher _dispatcher,
        AppDbContext _context) : IQueryHandler<GetRecurringIncomesQuery, RecurringIncomeListResponse>
    {
        public async Task<Result<RecurringIncomeListResponse>> Handle(GetRecurringIncomesQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<RecurringIncomeListResponse>();
            }

            List<RecurringIncomeRow> rows = await _context.RecurringIncomesOf(userId.Value)
                .Where(income => income.StoppedAt == null)
                .RecurringIncomeRows()
                .ToListAsync(cancellationToken);

            return Result.Ok(new RecurringIncomeListResponse(rows));
        }
    }
}
