namespace Kvit.Contracts.Categories
{
    public sealed record CategoryRow(Guid Id, string Key, string Emoji, string Color, int SortOrder);
}
