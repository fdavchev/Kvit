namespace Kvit.Contracts.PersonalEntries
{
    public sealed record LimitsResponse(LimitRow? Month, IReadOnlyList<LimitRow> Categories);
}
