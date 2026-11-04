using System.Globalization;
using Kvit.Domain.Results;
using Kvit.Domain.Statistics;
using Xunit;

namespace Kvit.Domain.Tests.Statistics
{
    public class StatsCalendarTests
    {
        [Theory]
        [InlineData("2026-11-02", "2026-11-02")]
        [InlineData("2026-11-04", "2026-11-02")]
        [InlineData("2026-11-08", "2026-11-02")]
        [InlineData("2026-11-09", "2026-11-09")]
        [InlineData("2026-01-01", "2025-12-29")]
        public void BucketStartOf_Week_IsTheMondayOnOrBeforeTheDay(string day, string expected)
        {
            DateOnly start = StatsCalendar.BucketStartOf(DateOf(day), StatsGrouping.Week);

            Assert.Equal(DateOf(expected), start);
        }

        [Theory]
        [InlineData("2026-11-01", "2026-11-01")]
        [InlineData("2026-11-30", "2026-11-01")]
        [InlineData("2028-02-29", "2028-02-01")]
        public void BucketStartOf_Month_IsTheFirstOfTheMonth(string day, string expected)
        {
            DateOnly start = StatsCalendar.BucketStartOf(DateOf(day), StatsGrouping.Month);

            Assert.Equal(DateOf(expected), start);
        }

        [Theory]
        [InlineData("2026-01-01", "2026-01-01")]
        [InlineData("2026-12-31", "2026-01-01")]
        public void BucketStartOf_Year_IsTheFirstOfJanuary(string day, string expected)
        {
            DateOnly start = StatsCalendar.BucketStartOf(DateOf(day), StatsGrouping.Year);

            Assert.Equal(DateOf(expected), start);
        }

        [Fact]
        public void BucketStartOf_Day_IsTheDayItself()
        {
            DateOnly start = StatsCalendar.BucketStartOf(DateOf("2026-11-04"), StatsGrouping.Day);

            Assert.Equal(DateOf("2026-11-04"), start);
        }

        [Fact]
        public void BucketStartsOf_WeekRangeStartingOnASunday_StartsWithTheWeekThatHoldsThatSunday()
        {
            IReadOnlyList<DateOnly> starts = StatsCalendar.BucketStartsOf(RangeOf("2026-11-01", "2026-11-04"), StatsGrouping.Week);

            Assert.Equal([DateOf("2026-10-26"), DateOf("2026-11-02")], starts);
        }

        [Fact]
        public void BucketStartsOf_MonthRangeAcrossNewYear_HasEveryMonthItTouches()
        {
            IReadOnlyList<DateOnly> starts = StatsCalendar.BucketStartsOf(RangeOf("2025-11-15", "2026-02-03"), StatsGrouping.Month);

            Assert.Equal([DateOf("2025-11-01"), DateOf("2025-12-01"), DateOf("2026-01-01"), DateOf("2026-02-01")], starts);
        }

        [Fact]
        public void BucketStartsOf_YearRange_HasEveryYearItTouches()
        {
            IReadOnlyList<DateOnly> starts = StatsCalendar.BucketStartsOf(RangeOf("2025-11-01", "2026-11-04"), StatsGrouping.Year);

            Assert.Equal([DateOf("2025-01-01"), DateOf("2026-01-01")], starts);
        }

        [Theory]
        [InlineData("2026-10-24", "2026-10-26")]
        [InlineData("2026-03-28", "2026-03-30")]
        public void BucketStartsOf_DayRangeAcrossAClockChange_HasOneBucketPerCalendarDay(string from, string to)
        {
            IReadOnlyList<DateOnly> starts = StatsCalendar.BucketStartsOf(RangeOf(from, to), StatsGrouping.Day);

            Assert.Equal([DateOf(from), DateOf(from).AddDays(1), DateOf(to)], starts);
        }

        [Theory]
        [InlineData("2026-11-03T22:59:59Z", "2026-11-03")]
        [InlineData("2026-11-03T23:00:00Z", "2026-11-04")]
        [InlineData("2026-10-25T22:59:59Z", "2026-10-25")]
        [InlineData("2026-10-25T23:00:00Z", "2026-10-26")]
        [InlineData("2026-03-29T21:59:59Z", "2026-03-29")]
        [InlineData("2026-03-29T22:00:00Z", "2026-03-30")]
        public void Today_IsTheCalendarDayInSkopjeIncludingOnClockChangeDays(string utcNow, string expected)
        {
            DateOnly today = StatsCalendar.Today(DateTimeOffset.Parse(utcNow, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));

            Assert.Equal(DateOf(expected), today);
        }

        [Theory]
        [InlineData("day", StatsGrouping.Day)]
        [InlineData("WeEk", StatsGrouping.Week)]
        [InlineData("MONTH", StatsGrouping.Month)]
        [InlineData("year", StatsGrouping.Year)]
        public void GroupingNamed_KnownNameInAnyCase_ReturnsTheGrouping(string name, StatsGrouping expected)
        {
            Result<StatsGrouping> result = StatsCalendar.GroupingNamed(name);

            Assert.True(result.IsSuccess);
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("days")]
        [InlineData("hour")]
        [InlineData("1")]
        [InlineData("")]
        public void GroupingNamed_UnknownName_FailsWithGroupingInvalid(string name)
        {
            Result<StatsGrouping> result = StatsCalendar.GroupingNamed(name);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.ADMIN_STATS_GROUPING_INVALID, result.ErrorCode);
        }

        [Fact]
        public void NameOf_EveryGrouping_IsItsLowercaseName()
        {
            List<string> names = [.. Enum.GetValues<StatsGrouping>().Select(StatsCalendar.NameOf)];

            Assert.Equal(["day", "week", "month", "year"], names);
        }

        private static DateOnly DateOf(string isoDate)
        {
            return DateOnly.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static StatsDateRange RangeOf(string from, string to)
        {
            return new StatsDateRange(DateOf(from), DateOf(to));
        }
    }
}
