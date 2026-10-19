using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Categories
{
    public sealed class ArchiveCategory(ICategoryRepository _categories, TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid ownerUserId, Guid categoryId, CancellationToken cancellationToken)
        {
            OwnedCategories owned = await _categories.OwnedByAsync(ownerUserId, cancellationToken);
            Result<Category> found = owned.Find(categoryId);
            if (!found.IsSuccess)
            {
                return found;
            }

            found.Value.Archive(_timeProvider.GetUtcNow());

            return Result.Ok();
        }
    }
}
