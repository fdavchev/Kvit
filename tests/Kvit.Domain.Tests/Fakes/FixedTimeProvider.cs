namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FixedTimeProvider(DateTimeOffset _now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}
