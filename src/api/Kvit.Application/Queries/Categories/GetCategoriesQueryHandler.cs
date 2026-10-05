using Kvit.Application.Dispatching;
using Kvit.Contracts.Categories;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Categories
{
    public sealed class GetCategoriesQueryHandler(AppDbContext _context) : IQueryHandler<GetCategoriesQuery, CategoryListResponse>
    {
        public async Task<Result<CategoryListResponse>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
        {
            List<CategoryRow> categories = await BuiltInCategoryRows().ToListAsync(cancellationToken);

            return Result.Ok(new CategoryListResponse(categories));
        }

        private IQueryable<CategoryRow> BuiltInCategoryRows()
        {
            return _context.Categories
                .AsNoTracking()
                .Where(category => category.OwnerUserId == null && category.ArchivedAt == null)
                .OrderBy(category => category.SortOrder)
                .Select(category => new CategoryRow(category.Id, category.Key!, category.Emoji, category.Color, category.SortOrder));
        }
    }
}
