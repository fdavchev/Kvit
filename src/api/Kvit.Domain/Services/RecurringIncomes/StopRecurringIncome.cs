using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.RecurringIncomes
{
    public sealed class StopRecurringIncome(
        IRecurringIncomeRepository _incomes,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid userId, Guid recurringIncomeId, CancellationToken cancellationToken)
        {
            Result<RecurringIncome> found = await _incomes.FindActiveOwnedAsync(userId, recurringIncomeId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            found.Value.Stop(_timeProvider.GetUtcNow());

            return Result.Ok();
        }
    }
}
