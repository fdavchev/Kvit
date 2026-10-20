using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Categories
{
    public sealed class CreateCategory(ICategoryRepository _categories)
    {
        public async Task<Result<Guid>> Execute(Guid ownerUserId, string name, string emoji, string color, CancellationToken cancellationToken)
        {
            Result<CategoryFields> fields = CategoryFields.Create(name, emoji, color);
            if (!fields.IsSuccess)
            {
                return fields.ToFailure<Guid>();
            }

            OwnedCategories owned = await _categories.OwnedByAsync(ownerUserId, cancellationToken);
            Category category = Category.CreateCustom(ownerUserId, fields.Value, owned.NextSortOrder());
            Result nameFree = owned.CheckNameFree(category.CustomName(), category.Id);
            if (!nameFree.IsSuccess)
            {
                return nameFree.ToFailure<Guid>();
            }

            _categories.Add(category);

            return Result.Ok(category.Id);
        }
    }
}
