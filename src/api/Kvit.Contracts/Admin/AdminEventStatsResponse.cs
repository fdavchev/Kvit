namespace Kvit.Contracts.Admin
{
    public sealed record AdminEventStatsResponse(int Total, int? PreviousTotal, IReadOnlyList<int> Counts);
}
