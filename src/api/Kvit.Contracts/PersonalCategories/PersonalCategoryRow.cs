namespace Kvit.Contracts.PersonalCategories
{
    public sealed record PersonalCategoryRow(Guid Id, string? Key, string? Name, string Emoji, string Color, int SortOrder, bool IsCustom, bool Archived);
}
