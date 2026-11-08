namespace Kvit.Infrastructure.Cleanup
{
    public sealed record RemovedGroups(int Groups, int Expenses, int Settlements)
    {
        public static readonly RemovedGroups None = new(0, 0, 0);

        public RemovedGroups Plus(RemovedGroups other)
        {
            return new RemovedGroups(Groups + other.Groups, Expenses + other.Expenses, Settlements + other.Settlements);
        }
    }
}
