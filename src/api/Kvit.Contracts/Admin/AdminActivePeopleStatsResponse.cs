namespace Kvit.Contracts.Admin
{
    public sealed record AdminActivePeopleStatsResponse(
        int People,
        int? PreviousPeople,
        int PersonDays,
        int? PreviousPersonDays,
        int Guests,
        IReadOnlyList<int> PeopleCounts,
        IReadOnlyList<int> PersonDayCounts);
}
