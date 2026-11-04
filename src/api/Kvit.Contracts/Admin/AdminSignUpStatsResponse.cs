namespace Kvit.Contracts.Admin
{
    public sealed record AdminSignUpStatsResponse(
        int Total,
        int? PreviousTotal,
        IReadOnlyList<int> Counts,
        int Email,
        int Google,
        int Guest,
        int GuestsUpgraded);
}
