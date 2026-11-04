namespace Kvit.Domain.Statistics
{
    public sealed record StatsDateRange(DateOnly From, DateOnly To)
    {
        public int DayCount()
        {
            return To.DayNumber - From.DayNumber + 1;
        }

        public StatsDateRange PreviousOfSameLength()
        {
            return new StatsDateRange(From.AddDays(-DayCount()), From.AddDays(-1));
        }
    }
}
