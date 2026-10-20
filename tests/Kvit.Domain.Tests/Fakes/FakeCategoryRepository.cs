using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [];

        public List<Category> Added { get; } = [];

        public void Seed(params Category[] categories)
        {
            _categories.AddRange(categories);
        }

        public void Add(Category category)
        {
            Added.Add(category);
        }

        public Task<Category?> FindAsync(Guid categoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_categories.SingleOrDefault(category => category.Id == categoryId));
        }

        public Task<OwnedCategories> OwnedByAsync(Guid ownerUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new OwnedCategories([.. _categories.Where(category => category.OwnerUserId == ownerUserId)]));
        }
    }
}
