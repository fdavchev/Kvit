using Kvit.Application.Dispatching;
using Kvit.Contracts.Admin;
using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Kvit.Domain.Statistics;

namespace Kvit.Application.Queries.Admin
{
    public sealed class GetAdminStatsQueryHandler(
        IUsageStatsRepository _usageStats,
        TimeProvider _timeProvider) : IQueryHandler<GetAdminStatsQuery, AdminStatsResponse>
    {
        public async Task<Result<AdminStatsResponse>> Handle(GetAdminStatsQuery query, CancellationToken cancellationToken)
        {
            DateOnly today = StatsCalendar.Today(_timeProvider.GetUtcNow());
            DateOnly? firstEventDay = query.From is null ? await _usageStats.FirstEventDayAsync(cancellationToken) : null;
            Result<StatsPeriod> created = StatsPeriod.Create(query.From, query.To, query.By, today, firstEventDay);
            if (!created.IsSuccess)
            {
                return created.ToFailure<AdminStatsResponse>();
            }

            StatsPeriod period = created.Value;
            IReadOnlyList<DateOnly> bucketStarts = period.BucketStarts();
            IReadOnlyList<UsageEventBucketCount> bucketCounts = await _usageStats.CountPerBucketAsync(period.Range, period.Grouping, cancellationToken);
            List<UsageEventCount> counts = [.. bucketCounts.Select(bucketCount => bucketCount.Events)];
            IReadOnlyList<UsageEventCount>? previousCounts = await PreviousCountsAsync(period, cancellationToken);
            ActivePeopleCount activePeople = await _usageStats.CountActivePeopleAsync(period.Range, cancellationToken);
            ActivePeopleCount? previousActivePeople = period.PreviousRange is null ? null : await _usageStats.CountActivePeopleAsync(period.PreviousRange, cancellationToken);
            IReadOnlyList<ActivePeopleBucketCount> activeBuckets = await _usageStats.CountActivePeoplePerBucketAsync(period.Range, period.Grouping, cancellationToken);
            IReadOnlyList<UsageEventCount> allTime = await _usageStats.CountAllTimeAsync(cancellationToken);
            AdminEventStatsResponse signUps = EventStatsOf(UsageEventType.SignedUp, bucketStarts, bucketCounts, previousCounts);

            return Result.Ok(new AdminStatsResponse(
                period.Range.From,
                period.Range.To,
                StatsCalendar.NameOf(period.Grouping),
                period.PreviousRange?.From,
                period.PreviousRange?.To,
                bucketStarts,
                new AdminAllTimeTotalsResponse(
                    SignUpsBy(allTime, SignUpMethod.Email) + SignUpsBy(allTime, SignUpMethod.Google),
                    SignUpsBy(allTime, SignUpMethod.Guest),
                    TotalOf(allTime, UsageEventType.GroupCreated),
                    TotalOf(allTime, UsageEventType.ExpenseAdded),
                    TotalOf(allTime, UsageEventType.SettlementConfirmed)),
                new AdminSignUpStatsResponse(
                    signUps.Total,
                    signUps.PreviousTotal,
                    signUps.Counts,
                    SignUpsBy(counts, SignUpMethod.Email),
                    SignUpsBy(counts, SignUpMethod.Google),
                    SignUpsBy(counts, SignUpMethod.Guest),
                    TotalOf(counts, UsageEventType.GuestUpgraded)),
                new AdminActivePeopleStatsResponse(
                    activePeople.People,
                    previousActivePeople?.People,
                    activePeople.PersonDays,
                    previousActivePeople?.PersonDays,
                    activePeople.Guests,
                    SpreadOverBuckets(bucketStarts, activeBuckets.Select(bucket => (bucket.BucketStart, bucket.People))),
                    SpreadOverBuckets(bucketStarts, activeBuckets.Select(bucket => (bucket.BucketStart, bucket.PersonDays)))),
                EventStatsOf(UsageEventType.JoinedViaInvite, bucketStarts, bucketCounts, previousCounts),
                EventStatsOf(UsageEventType.GroupCreated, bucketStarts, bucketCounts, previousCounts),
                EventStatsOf(UsageEventType.ExpenseAdded, bucketStarts, bucketCounts, previousCounts),
                EventStatsOf(UsageEventType.SettlementConfirmed, bucketStarts, bucketCounts, previousCounts)));
        }

        private async Task<IReadOnlyList<UsageEventCount>?> PreviousCountsAsync(StatsPeriod period, CancellationToken cancellationToken)
        {
            if (period.PreviousRange is null)
            {
                return null;
            }

            IReadOnlyList<UsageEventBucketCount> previousBucketCounts = await _usageStats.CountPerBucketAsync(period.PreviousRange, period.Grouping, cancellationToken);

            return [.. previousBucketCounts.Select(bucketCount => bucketCount.Events)];
        }

        private static AdminEventStatsResponse EventStatsOf(
            UsageEventType type,
            IReadOnlyList<DateOnly> bucketStarts,
            IReadOnlyList<UsageEventBucketCount> bucketCounts,
            IReadOnlyList<UsageEventCount>? previousCounts)
        {
            List<UsageEventBucketCount> ofType = [.. bucketCounts.Where(bucketCount => bucketCount.Events.Type == type)];

            return new AdminEventStatsResponse(
                TotalOf(ofType.Select(bucketCount => bucketCount.Events), type),
                previousCounts is null ? null : TotalOf(previousCounts, type),
                SpreadOverBuckets(bucketStarts, ofType.Select(bucketCount => (bucketCount.BucketStart, bucketCount.Events.Count))));
        }

        private static int TotalOf(IEnumerable<UsageEventCount> counts, UsageEventType type)
        {
            return counts.Where(count => count.Type == type).Sum(count => count.Count);
        }

        private static int SignUpsBy(IEnumerable<UsageEventCount> counts, SignUpMethod method)
        {
            return counts
                .Where(count => count.Type == UsageEventType.SignedUp && count.Detail == method.ToString())
                .Sum(count => count.Count);
        }

        private static List<int> SpreadOverBuckets(IReadOnlyList<DateOnly> bucketStarts, IEnumerable<(DateOnly BucketStart, int Count)> counts)
        {
            Dictionary<DateOnly, int> positions = bucketStarts
                .Select((bucketStart, position) => (bucketStart, position))
                .ToDictionary(entry => entry.bucketStart, entry => entry.position);
            int[] spread = new int[bucketStarts.Count];
            foreach ((DateOnly bucketStart, int count) in counts)
            {
                if (!positions.TryGetValue(bucketStart, out int position))
                {
                    throw new InvalidOperationException(
                        $"The database counted events in a bucket starting {bucketStart}, which is not one of the buckets {bucketStarts[0]}..{bucketStarts[^1]}.");
                }

                spread[position] += count;
            }

            return [.. spread];
        }
    }
}
