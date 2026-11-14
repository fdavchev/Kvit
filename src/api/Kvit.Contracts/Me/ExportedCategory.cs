namespace Kvit.Contracts.Me
{
    public sealed record ExportedCategory(Guid Id, string Name, string Emoji, string Color, int SortOrder, DateTimeOffset? ArchivedAt);
}
