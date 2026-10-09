using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class CategoryRepository(AppDbContext _context) : ICategoryRepository
    {
        public Task<Category?> FindAsync(Guid categoryId, CancellationToken cancellationToken)
        {
            return _context.Categories
                .AsNoTracking()
                .SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);
        }
    }
}
