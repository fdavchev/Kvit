using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Categories
{
    public sealed class UpdateCategory(ICategoryRepository _categories)
    {
        public async Task<Result> Execute(Guid ownerUserId, Guid categoryId, string name, string emoji, string color, CancellationToken cancellationToken)
        {
            OwnedCategories owned = await _categories.OwnedByAsync(ownerUserId, cancellationToken);
            Result<Category> found = owned.Find(categoryId);
            if (!found.IsSuccess)
            {
                return found;
            }

            Result<CategoryFields> fields = CategoryFields.Create(name, emoji, color);
            if (!fields.IsSuccess)
            {
                return fields;
            }

            Category category = found.Value;
            if (category.ArchivedAt is null)
            {
                Result nameFree = owned.CheckNameFree(fields.Value.Name, category.Id);
                if (!nameFree.IsSuccess)
                {
                    return nameFree;
                }
            }

            category.Update(fields.Value);

            return Result.Ok();
        }
    }
}
