using Kvit.Domain.Entities;
using Kvit.Domain.Results;

namespace Kvit.Domain.Interfaces
{
    public interface IRecurringIncomeRepository
    {
        void Add(RecurringIncome income);

        Task<RecurringIncome?> FindByClientRequestIdAsync(Guid clientRequestId, CancellationToken cancellationToken);

        Task<Result<RecurringIncome>> FindActiveOwnedAsync(Guid userId, Guid recurringIncomeId, CancellationToken cancellationToken);

        Task<Result<RecurringIncome>> FindOwnedIncludingStoppedAsync(Guid userId, Guid recurringIncomeId, CancellationToken cancellationToken);

        Task<int> LockAndCountActiveOfAsync(Guid userId, CancellationToken cancellationToken);
    }
}
