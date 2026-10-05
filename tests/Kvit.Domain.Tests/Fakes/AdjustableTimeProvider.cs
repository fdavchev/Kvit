namespace Kvit.Domain.Tests.Fakes
{
    public sealed class AdjustableTimeProvider(DateTimeOffset _now) : TimeProvider
    {
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
