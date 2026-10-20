using Kvit.Domain.Entities;
using Kvit.Domain.Results;

namespace Kvit.Domain.Interfaces
{
    public interface IPersonalEntryRepository
    {
        void Add(PersonalEntry entry);

        Task<PersonalEntry?> FindByClientRequestIdAsync(Guid clientRequestId, CancellationToken cancellationToken);

        Task<Result<PersonalEntry>> FindOwnedAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);

        Task<Result<PersonalEntry>> FindOwnedIncludingDeletedAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);

        Task<bool> AnyFromRecurringIncomeAsync(Guid recurringIncomeId, CancellationToken cancellationToken);
    }
}
