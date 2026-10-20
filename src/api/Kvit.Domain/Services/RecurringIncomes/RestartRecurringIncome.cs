using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.RecurringIncomes
{
    public sealed class RestartRecurringIncome(
        IRecurringIncomeRepository _incomes,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid userId, Guid recurringIncomeId, CancellationToken cancellationToken)
        {
            Result<RecurringIncome> found = await _incomes.FindOwnedIncludingStoppedAsync(userId, recurringIncomeId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Result restarted = found.Value.Restart(_timeProvider.GetUtcNow());
            if (!restarted.IsSuccess)
            {
                return restarted;
            }

            int activeCount = await _incomes.LockAndCountActiveOfAsync(userId, cancellationToken);

            return RecurringIncome.CheckRoomForOneMore(activeCount);
        }
    }
}
