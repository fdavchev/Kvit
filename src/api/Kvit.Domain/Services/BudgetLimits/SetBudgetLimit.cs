using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.BudgetLimits
{
    public sealed class SetBudgetLimit(
        IBudgetLimitRepository _limits,
        ICategoryRepository _categories,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid userId, string timeZoneId, Guid? categoryId, long? limitMinor, CancellationToken cancellationToken)
        {
            if (categoryId is Guid id)
            {
                Result category = await _categories.CheckBudgetLimitCategoryAsync(userId, id, cancellationToken);
                if (!category.IsSuccess)
                {
                    return category;
                }
            }

            Result<long> amount = BudgetLimit.CheckAmount(limitMinor);
            if (!amount.IsSuccess)
            {
                return amount;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateOnly currentMonth = BudgetLimit.FirstDayOfMonthOf(UserLocalDate.Today(now, timeZoneId));
            BudgetLimitHistory history = await _limits.HistoryOfAsync(userId, categoryId, cancellationToken);
            BudgetLimit? currentMonthRow = history.RowOf(currentMonth);
            if (currentMonthRow is null)
            {
                _limits.Add(BudgetLimit.Create(userId, categoryId, currentMonth, amount.Value, now));
            }
            else
            {
                currentMonthRow.Change(amount.Value, now);
            }

            return Result.Ok();
        }
    }
}
