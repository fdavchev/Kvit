using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface ICategoryRepository
    {
        Task<Category?> FindAsync(Guid categoryId, CancellationToken cancellationToken);
    }
}
