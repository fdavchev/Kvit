namespace Kvit.Contracts.Dashboard
{
    public sealed record ClosingNeedsYouItem(Guid GroupId, string GroupName, string StartedByName)
        : NeedsYouItem(GroupId, GroupName);
}
