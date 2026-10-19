using Kvit.Contracts.PersonalCategories;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record PersonalCategoryTotalRow(Guid CategoryId, PersonalCategoryRow Category, long TotalMinor, int Count);
}
