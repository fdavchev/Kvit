using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface IDuplicateAnswerRepository
    {
        Task SaveAsync(DuplicateAnswer answer, CancellationToken cancellationToken);

        Task RemoveSameAnswersOfEntryAsync(Guid userId, Guid personalEntryId, CancellationToken cancellationToken);

        Task RemoveEndedMergesOfEitherAsync(Guid userId, Guid personalEntryId, Guid expenseId, CancellationToken cancellationToken);

        Task<bool> IsEitherMergedWithAnotherAsync(Guid userId, Guid personalEntryId, Guid expenseId, CancellationToken cancellationToken);
    }
}
