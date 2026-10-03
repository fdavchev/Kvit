namespace Kvit.Contracts.Groups
{
    public sealed record GroupListResponse(
        IReadOnlyList<GroupListRow> Groups,
        IReadOnlyList<GroupListRow> FinishedGroups,
        IReadOnlyList<DeletedGroupListRow> RecentlyDeleted);
}
