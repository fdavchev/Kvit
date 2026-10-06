using Kvit.Domain.Entities;
using Kvit.Domain.Results;

namespace Kvit.Domain.Interfaces
{
    public static class CategoryRepositoryExtensions
    {
        public static async Task<Result<string>> GroupExpenseKeyAsync(this ICategoryRepository categories, Guid? categoryId, CancellationToken cancellationToken)
        {
            if (categoryId is not Guid id)
            {
                return Result.Ok(string.Empty);
            }

            Category? category = await categories.FindAsync(id, cancellationToken);
            if (category is null || !category.CanTagGroupExpenses())
            {
                return Result.Failure<string>($"Category {id} is not a built-in category that group expenses can use.", ResultCodes.EXPENSE_CATEGORY_INVALID);
            }

            return Result.Ok(category.BuiltInKey());
        }

        public static async Task<string> StoredKeyAsync(this ICategoryRepository categories, Guid? categoryId, CancellationToken cancellationToken)
        {
            if (categoryId is not Guid id)
            {
                return string.Empty;
            }

            Category category = await categories.FindAsync(id, cancellationToken)
                ?? throw new InvalidOperationException($"Category {id} is on an expense but does not exist.");

            return category.BuiltInKey();
        }
    }
}
