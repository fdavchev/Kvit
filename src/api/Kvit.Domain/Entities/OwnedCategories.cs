using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class OwnedCategories(IReadOnlyList<Category> _categories)
    {
        public Result<Category> Find(Guid categoryId)
        {
            Category? category = _categories.SingleOrDefault(candidate => candidate.Id == categoryId);
            if (category is null)
            {
                return Category.NotFound<Category>(categoryId);
            }

            return Result.Ok(category);
        }

        public int NextSortOrder()
        {
            return _categories.Select(category => category.SortOrder).Append(Category.LastBuiltInSortOrder).Max() + 1;
        }

        public Result CheckNameFree(string name, Guid exceptCategoryId)
        {
            bool isTaken = _categories.Any(category =>
                category.Id != exceptCategoryId
                && category.ArchivedAt is null
                && string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase));
            if (isTaken)
            {
                return Result.Failure($"You already have a category called {name}.", ResultCodes.CATEGORY_NAME_TAKEN);
            }

            return Result.Ok();
        }
    }
}
