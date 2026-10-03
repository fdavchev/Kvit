namespace Kvit.Contracts.Groups
{
    public sealed record GroupMembersResponse(
        IReadOnlyList<GroupMemberRow> Members,
        IReadOnlyList<RemovedMemberRow> Removed,
        bool CanClaimNames);
}
