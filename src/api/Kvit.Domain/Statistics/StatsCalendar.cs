using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Domain.Statistics
{
    public static class StatsCalendar
    {
        public const string TimeZoneId = "Europe/Skopje";

        public static DateOnly Today(DateTimeOffset utcNow)
        {
            return UserLocalDate.Today(utcNow, TimeZoneId);
        }

        public static string NameOf(StatsGrouping grouping)
        {
            return grouping.ToString().ToLowerInvariant();
        }

        public static Result<StatsGrouping> GroupingNamed(string name)
        {
            foreach (StatsGrouping grouping in Enum.GetValues<StatsGrouping>())
            {
                if (string.Equals(NameOf(grouping), name, StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Ok(grouping);
                }
            }

            return Result.Failure<StatsGrouping>($"The grouping '{name}' is not known. Use day, week, month or year.", ResultCodes.ADMIN_STATS_GROUPING_INVALID);
        }

        public static DateOnly BucketStartOf(DateOnly day, StatsGrouping grouping)
        {
            return grouping switch
            {
                StatsGrouping.Day => day,
                StatsGrouping.Week => day.AddDays(-DaysSinceMonday(day)),
                StatsGrouping.Month => new DateOnly(day.Year, day.Month, 1),
                StatsGrouping.Year => new DateOnly(day.Year, 1, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "This grouping has no bucket rule."),
            };
        }

        public static IReadOnlyList<DateOnly> BucketStartsOf(StatsDateRange range, StatsGrouping grouping)
        {
            List<DateOnly> starts = [];
            for (DateOnly start = BucketStartOf(range.From, grouping); start <= range.To; start = NextBucketStart(start, grouping))
            {
                starts.Add(start);
            }

            return starts;
        }

        private static DateOnly NextBucketStart(DateOnly bucketStart, StatsGrouping grouping)
        {
            return grouping switch
            {
                StatsGrouping.Day => bucketStart.AddDays(1),
                StatsGrouping.Week => bucketStart.AddDays(7),
                StatsGrouping.Month => bucketStart.AddMonths(1),
                StatsGrouping.Year => bucketStart.AddYears(1),
                _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "This grouping has no bucket rule."),
            };
        }

        private static int DaysSinceMonday(DateOnly day)
        {
            return ((int)day.DayOfWeek + 6) % 7;
        }
    }
}
