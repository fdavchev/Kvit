using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Me;
using Kvit.Contracts.RecurringIncomes;
using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.RecurringIncomes
{
    public sealed class GetRecurringIncomeQueryHandler(
        IDispatcher _dispatcher,
        AppDbContext _context,
        TimeProvider _timeProvider) : IQueryHandler<GetRecurringIncomeQuery, RecurringIncomeRow>
    {
        public async Task<Result<RecurringIncomeRow>> Handle(GetRecurringIncomeQuery query, CancellationToken cancellationToken)
        {
            Result<MeResponse> caller = await _dispatcher.FullAccountOfCallerAsync(cancellationToken);
            if (!caller.IsSuccess)
            {
                return caller.ToFailure<RecurringIncomeRow>();
            }

            DateOnly userToday = UserLocalDate.Today(_timeProvider.GetUtcNow(), caller.Value.TimeZone);
            RecurringIncomeRow? row = await _context.RecurringIncomesOf(caller.Value.Id)
                .Where(income => income.Id == query.RecurringIncomeId)
                .RecurringIncomeRows(_context.PersonalEntries, userToday)
                .SingleOrDefaultAsync(cancellationToken);
            if (row is null)
            {
                return RecurringIncome.NotFound<RecurringIncomeRow>(query.RecurringIncomeId);
            }

            return Result.Ok(row);
        }
    }
}
