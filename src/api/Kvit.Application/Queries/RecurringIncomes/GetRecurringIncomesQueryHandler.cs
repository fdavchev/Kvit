using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Me;
using Kvit.Contracts.RecurringIncomes;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.RecurringIncomes
{
    public sealed class GetRecurringIncomesQueryHandler(
        IDispatcher _dispatcher,
        AppDbContext _context,
        TimeProvider _timeProvider) : IQueryHandler<GetRecurringIncomesQuery, RecurringIncomeListResponse>
    {
        public async Task<Result<RecurringIncomeListResponse>> Handle(GetRecurringIncomesQuery query, CancellationToken cancellationToken)
        {
            Result<MeResponse> caller = await _dispatcher.FullAccountOfCallerAsync(cancellationToken);
            if (!caller.IsSuccess)
            {
                return caller.ToFailure<RecurringIncomeListResponse>();
            }

            DateOnly userToday = UserLocalDate.Today(_timeProvider.GetUtcNow(), caller.Value.TimeZone);
            List<RecurringIncomeRow> rows = await _context.RecurringIncomesOf(caller.Value.Id)
                .Where(income => income.StoppedAt == null)
                .RecurringIncomeRows(_context.PersonalEntries, userToday)
                .ToListAsync(cancellationToken);

            return Result.Ok(new RecurringIncomeListResponse(rows));
        }
    }
}
