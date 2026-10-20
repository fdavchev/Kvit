namespace Kvit.Contracts.PersonalCategories
{
    public sealed record PersonalCategoryListResponse(IReadOnlyList<PersonalCategoryRow> Categories);
}
