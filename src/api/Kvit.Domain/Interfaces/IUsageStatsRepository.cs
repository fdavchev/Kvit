using Kvit.Domain.Statistics;

namespace Kvit.Domain.Interfaces
{
    public interface IUsageStatsRepository
    {
        Task<DateOnly?> FirstEventDayAsync(CancellationToken cancellationToken);

        Task<IReadOnlyList<UsageEventCount>> CountAllTimeAsync(CancellationToken cancellationToken);

        Task<IReadOnlyList<UsageEventBucketCount>> CountPerBucketAsync(StatsDateRange range, StatsGrouping grouping, CancellationToken cancellationToken);

        Task<ActivePeopleCount> CountActivePeopleAsync(StatsDateRange range, CancellationToken cancellationToken);

        Task<IReadOnlyList<ActivePeopleBucketCount>> CountActivePeoplePerBucketAsync(StatsDateRange range, StatsGrouping grouping, CancellationToken cancellationToken);
    }
}
