using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.BudgetLimits
{
    public sealed class RemoveBudgetLimit(
        IBudgetLimitRepository _limits,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid userId, string timeZoneId, Guid? categoryId, CancellationToken cancellationToken)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateOnly currentMonth = BudgetLimit.FirstDayOfMonthOf(UserLocalDate.Today(now, timeZoneId));
            BudgetLimitHistory history = await _limits.HistoryOfAsync(userId, categoryId, cancellationToken);
            BudgetLimit? currentMonthRow = history.RowOf(currentMonth);

            if (!history.HasLimitInForceBefore(currentMonth))
            {
                if (currentMonthRow is not null)
                {
                    _limits.Remove(currentMonthRow);
                }

                return Result.Ok();
            }

            if (currentMonthRow is null)
            {
                _limits.Add(BudgetLimit.Create(userId, categoryId, currentMonth, null, now));
            }
            else
            {
                currentMonthRow.Change(null, now);
            }

            return Result.Ok();
        }
    }
}
