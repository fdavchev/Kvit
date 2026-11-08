namespace Kvit.Api.Tests.Cleanup
{
    public sealed record GroupRowCounts(
        int Groups,
        int Members,
        int Expenses,
        int Shares,
        int Settlements,
        int ClosingResponses,
        int ActivityEvents)
    {
        public static readonly GroupRowCounts None = new(0, 0, 0, 0, 0, 0, 0);
    }
}
