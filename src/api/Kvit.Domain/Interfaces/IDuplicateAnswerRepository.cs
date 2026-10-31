using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface IDuplicateAnswerRepository
    {
        Task SaveAsync(DuplicateAnswer answer, CancellationToken cancellationToken);

        Task RemoveAsync(Guid userId, Guid personalEntryId, Guid expenseId, CancellationToken cancellationToken);
    }
}
