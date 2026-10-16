namespace Kvit.Contracts.Dashboard
{
    public sealed record DashboardResponse(
        DashboardTotalsResponse Totals,
        IReadOnlyList<DashboardGroupRow> Groups,
        IReadOnlyList<NeedsYouItem> NeedsYou,
        IReadOnlyList<DashboardActivityRow> RecentActivity,
        PlusTargetResponse? PlusTarget);
}
