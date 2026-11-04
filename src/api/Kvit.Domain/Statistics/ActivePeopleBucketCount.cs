namespace Kvit.Domain.Statistics
{
    public sealed record ActivePeopleBucketCount(DateOnly BucketStart, int People, int PersonDays);
}
