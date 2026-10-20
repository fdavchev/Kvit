using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface ICategoryRepository
    {
        void Add(Category category);

        Task<Category?> FindAsync(Guid categoryId, CancellationToken cancellationToken);

        Task<OwnedCategories> OwnedByAsync(Guid ownerUserId, CancellationToken cancellationToken);
    }
}
