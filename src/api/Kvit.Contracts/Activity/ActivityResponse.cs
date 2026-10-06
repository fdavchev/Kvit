namespace Kvit.Contracts.Activity
{
    public sealed record ActivityResponse(IReadOnlyList<ActivityEventRow> Events);
}
