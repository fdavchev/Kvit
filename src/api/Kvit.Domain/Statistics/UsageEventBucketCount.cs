namespace Kvit.Domain.Statistics
{
    public sealed record UsageEventBucketCount(DateOnly BucketStart, UsageEventCount Events);
}
