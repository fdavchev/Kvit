using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.RecurringIncomes
{
    public sealed class EditRecurringIncome(
        IRecurringIncomeRepository _incomes,
        IPersonalEntryRepository _entries,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid userId, Guid recurringIncomeId, RecurringIncomeInput input, CancellationToken cancellationToken)
        {
            Result<RecurringIncome> found = await _incomes.FindActiveOwnedAsync(userId, recurringIncomeId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            bool hasEntries = await _entries.AnyFromRecurringIncomeAsync(recurringIncomeId, cancellationToken);
            DateOnly utcToday = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

            return found.Value.Update(input, hasEntries, utcToday);
        }
    }
}
