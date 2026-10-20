using Kvit.Contracts.PersonalCategories;
using Kvit.Domain.Entities;

namespace Kvit.Application.Queries.PersonalCategories
{
    public static class PersonalCategoryQueries
    {
        public static IQueryable<PersonalCategoryRow> PersonalCategoryRows(this IQueryable<Category> categories)
        {
            return categories
                .OrderBy(category => category.OwnerUserId != null)
                .ThenBy(category => category.SortOrder)
                .ThenBy(category => category.Id)
                .Select(category => new PersonalCategoryRow(
                    category.Id,
                    category.Key,
                    category.Name,
                    category.Emoji,
                    category.Color,
                    category.SortOrder,
                    category.OwnerUserId != null,
                    category.ArchivedAt != null));
        }
    }
}
