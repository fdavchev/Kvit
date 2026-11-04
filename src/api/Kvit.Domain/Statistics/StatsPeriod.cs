using Kvit.Domain.Results;

namespace Kvit.Domain.Statistics
{
    public sealed class StatsPeriod
    {
        public const int MaxDaysGroupedByDay = 92;

        public static readonly DateOnly EarliestDay = new(2026, 1, 1);

        private StatsPeriod(StatsDateRange range, StatsGrouping grouping, StatsDateRange? previousRange)
        {
            Range = range;
            Grouping = grouping;
            PreviousRange = previousRange;
        }

        public StatsDateRange Range { get; }

        public StatsGrouping Grouping { get; }

        public StatsDateRange? PreviousRange { get; }

        public static Result<StatsPeriod> Create(DateOnly? from, DateOnly? to, string? groupingName, DateOnly today, DateOnly? firstEventDay)
        {
            Result<StatsGrouping> grouping = groupingName is null ? Result.Ok(StatsGrouping.Day) : StatsCalendar.GroupingNamed(groupingName);
            if (!grouping.IsSuccess)
            {
                return grouping.ToFailure<StatsPeriod>();
            }

            DateOnly lastDay = to ?? today;
            if (lastDay > today || from > today)
            {
                return Result.Failure<StatsPeriod>($"The range {from}..{lastDay} reaches past today in Skopje ({today}).", ResultCodes.ADMIN_STATS_FUTURE_DATE);
            }

            if (from > lastDay)
            {
                return Result.Failure<StatsPeriod>($"The range starts on {from}, after its last day {lastDay}.", ResultCodes.ADMIN_STATS_RANGE_INVALID);
            }

            if (from < EarliestDay)
            {
                return Result.Failure<StatsPeriod>($"The range starts on {from}, before statistics start on {EarliestDay}.", ResultCodes.ADMIN_STATS_DATE_TOO_OLD);
            }

            StatsDateRange range = new(from ?? FirstDayOfEverything(firstEventDay, lastDay), lastDay);
            if (grouping.Value == StatsGrouping.Day && range.DayCount() > MaxDaysGroupedByDay)
            {
                return Result.Failure<StatsPeriod>(
                    $"The range {range.From}..{range.To} has {range.DayCount()} days; grouping by day allows at most {MaxDaysGroupedByDay}.",
                    ResultCodes.ADMIN_STATS_DAY_RANGE_TOO_LONG);
            }

            StatsDateRange? previousRange = from is null ? null : range.PreviousOfSameLength();

            return Result.Ok(new StatsPeriod(range, grouping.Value, previousRange));
        }

        public IReadOnlyList<DateOnly> BucketStarts()
        {
            return StatsCalendar.BucketStartsOf(Range, Grouping);
        }

        private static DateOnly FirstDayOfEverything(DateOnly? firstEventDay, DateOnly lastDay)
        {
            return firstEventDay is { } firstDay && firstDay < lastDay ? firstDay : lastDay;
        }
    }
}
