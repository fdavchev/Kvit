namespace Kvit.Api.Tests.Expenses
{
    public sealed class AdjustableClock(DateTimeOffset _now) : TimeProvider
    {
        public DateOnly UtcToday => DateOnly.FromDateTime(_now.UtcDateTime);

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }

        public void Advance(TimeSpan duration)
        {
            _now += duration;
        }
    }
}
