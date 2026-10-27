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

        public static async Task<Result> CheckPersonalEntryCategoryAsync(this ICategoryRepository categories, Guid userId, Guid categoryId, CancellationToken cancellationToken)
        {
            Category? category = await categories.FindAsync(categoryId, cancellationToken);
            if (category is null || !category.CanTagPersonalEntriesOf(userId))
            {
                return Result.Failure($"Category {categoryId} is neither a built-in category nor one of your own active categories.", ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            }

            return Result.Ok();
        }

        public static async Task<Result> CheckBudgetLimitCategoryAsync(this ICategoryRepository categories, Guid userId, Guid categoryId, CancellationToken cancellationToken)
        {
            Category? category = await categories.FindAsync(categoryId, cancellationToken);
            if (category is null || !category.CanHaveALimitOf(userId))
            {
                return Result.NotFound($"Category {categoryId} is neither a built-in category nor one of your own categories.", ResultCodes.CATEGORY_NOT_FOUND);
            }

            return Result.Ok();
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
