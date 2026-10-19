using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class CategoryRepository(AppDbContext _context) : ICategoryRepository
    {
        public void Add(Category category)
        {
            _context.Categories.Add(category);
        }

        public Task<Category?> FindAsync(Guid categoryId, CancellationToken cancellationToken)
        {
            return _context.Categories
                .AsNoTracking()
                .SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);
        }

        public async Task<OwnedCategories> OwnedByAsync(Guid ownerUserId, CancellationToken cancellationToken)
        {
            List<Category> categories = await _context.Categories
                .Where(category => category.OwnerUserId == ownerUserId)
                .ToListAsync(cancellationToken);

            return new OwnedCategories(categories);
        }
    }
}
