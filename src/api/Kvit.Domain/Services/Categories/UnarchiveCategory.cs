using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Categories
{
    public sealed class UnarchiveCategory(ICategoryRepository _categories)
    {
        public async Task<Result> Execute(Guid ownerUserId, Guid categoryId, CancellationToken cancellationToken)
        {
            OwnedCategories owned = await _categories.OwnedByAsync(ownerUserId, cancellationToken);
            Result<Category> found = owned.Find(categoryId);
            if (!found.IsSuccess)
            {
                return found;
            }

            Category category = found.Value;
            if (category.ArchivedAt is null)
            {
                return Result.Ok();
            }

            Result nameFree = owned.CheckNameFree(category.CustomName(), category.Id);
            if (!nameFree.IsSuccess)
            {
                return nameFree;
            }

            category.Unarchive();

            return Result.Ok();
        }
    }
}
