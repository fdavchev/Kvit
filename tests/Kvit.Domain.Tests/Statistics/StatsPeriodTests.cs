using System.Globalization;
using Kvit.Domain.Results;
using Kvit.Domain.Statistics;
using Xunit;

namespace Kvit.Domain.Tests.Statistics
{
    public class StatsPeriodTests
    {
        private static readonly DateOnly Today = DateOf("2026-11-04");

        [Fact]
        public void Create_NothingGiven_IsDayGroupingFromTheFirstEventDayToTodayWithoutAPreviousPeriod()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(null, null, null, Today, DateOf("2026-11-01"));

            Assert.True(result.IsSuccess);
            Assert.Equal(RangeOf("2026-11-01", "2026-11-04"), result.Value.Range);
            Assert.Equal(StatsGrouping.Day, result.Value.Grouping);
            Assert.Null(result.Value.PreviousRange);
        }

        [Fact]
        public void Create_FromOmittedAndNoEvents_StartsAndEndsOnTo()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(null, DateOf("2026-10-20"), null, Today, null);

            Assert.Equal(RangeOf("2026-10-20", "2026-10-20"), result.Value.Range);
        }

        [Fact]
        public void Create_FromOmittedAndTheFirstEventIsAfterTo_StartsOnTo()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(null, DateOf("2026-10-20"), null, Today, DateOf("2026-11-01"));

            Assert.Equal(RangeOf("2026-10-20", "2026-10-20"), result.Value.Range);
        }

        [Theory]
        [InlineData("2026-11-01", "2026-11-04", "2026-10-28", "2026-10-31")]
        [InlineData("2026-11-04", "2026-11-04", "2026-11-03", "2026-11-03")]
        [InlineData("2026-10-01", "2026-10-31", "2026-08-31", "2026-09-30")]
        [InlineData("2028-03-01", "2028-03-31", "2028-01-30", "2028-02-29")]
        public void Create_FromGiven_PreviousPeriodHasTheSameNumberOfDaysRightBefore(string from, string to, string previousFrom, string previousTo)
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf(from), DateOf(to), "month", DateOf("2028-12-31"), null);

            Assert.Equal(RangeOf(previousFrom, previousTo), result.Value.PreviousRange);
        }

        [Fact]
        public void Create_ToAfterToday_FailsWithFutureDate()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2026-11-01"), DateOf("2026-11-05"), null, Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_FUTURE_DATE, result.ErrorCode);
        }

        [Fact]
        public void Create_FromAfterTodayAndToOmitted_FailsWithFutureDateNotRangeInvalid()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2026-11-05"), null, null, Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_FUTURE_DATE, result.ErrorCode);
        }

        [Fact]
        public void Create_FromAfterTo_FailsWithRangeInvalid()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2026-11-03"), DateOf("2026-11-01"), null, Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_RANGE_INVALID, result.ErrorCode);
        }

        [Fact]
        public void Create_UnknownGroupingAndBadDates_FailsWithGroupingInvalidFirst()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2026-11-03"), DateOf("2026-11-01"), "hour", Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_GROUPING_INVALID, result.ErrorCode);
        }

        [Fact]
        public void Create_DayGroupingOfExactly92Days_Succeeds()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2026-08-05"), Today, "day", Today, null);

            Assert.True(result.IsSuccess);
            Assert.Equal(92, result.Value.BucketStarts().Count);
        }

        [Fact]
        public void Create_DayGroupingOf93Days_FailsWithDayRangeTooLong()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2026-08-04"), Today, null, Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_DAY_RANGE_TOO_LONG, result.ErrorCode);
        }

        [Fact]
        public void Create_WeekGroupingOf93Days_Succeeds()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2026-08-04"), Today, "week", Today, null);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Create_FromOmittedAndTheFirstEventMoreThan92DaysAgo_FailsWithDayRangeTooLong()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(null, null, null, Today, DateOf("2026-01-01"));

            Assert.Equal(ResultCodes.ADMIN_STATS_DAY_RANGE_TOO_LONG, result.ErrorCode);
        }

        [Fact]
        public void EarliestDay_Is1January2026()
        {
            Assert.Equal(DateOf("2026-01-01"), StatsPeriod.EarliestDay);
        }

        [Fact]
        public void Create_FromTheDayBeforeTheEarliestDay_FailsWithDateTooOld()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2025-12-31"), Today, "month", Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_DATE_TOO_OLD, result.ErrorCode);
        }

        [Theory]
        [InlineData("0001-01-01")]
        [InlineData("0002-11-04")]
        [InlineData("0020-11-04")]
        [InlineData("0202-11-04")]
        public void Create_FromInAYearTypedOnlyInPart_FailsWithDateTooOld(string from)
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf(from), Today, "week", Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_DATE_TOO_OLD, result.ErrorCode);
        }

        [Fact]
        public void Create_FromOnTheEarliestDay_SucceedsWithThePreviousPeriodRightBefore()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2026-01-01"), Today, "month", Today, null);

            Assert.True(result.IsSuccess);
            Assert.Equal(RangeOf("2026-01-01", "2026-11-04"), result.Value.Range);
            Assert.Equal(RangeOf("2025-02-27", "2025-12-31"), result.Value.PreviousRange);
        }

        [Fact]
        public void Create_FromOnTheEarliestDayAndAFarLaterToday_HasThePreviousPeriodOfTheSameLength()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2026-01-01"), DateOf("2099-12-31"), "year", DateOf("2099-12-31"), null);

            Assert.True(result.IsSuccess);
            Assert.Equal(result.Value.Range.DayCount(), result.Value.PreviousRange!.DayCount());
            Assert.Equal(DateOf("2025-12-31"), result.Value.PreviousRange.To);
        }

        [Fact]
        public void Create_UnknownGroupingAndFromTooOld_FailsWithGroupingInvalidFirst()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2025-12-31"), Today, "hour", Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_GROUPING_INVALID, result.ErrorCode);
        }

        [Fact]
        public void Create_ToAfterTodayAndFromTooOld_FailsWithFutureDateFirst()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2025-12-31"), DateOf("2026-11-05"), "month", Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_FUTURE_DATE, result.ErrorCode);
        }

        [Fact]
        public void Create_FromAfterToAndBothTooOld_FailsWithRangeInvalidFirst()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2025-12-31"), DateOf("2025-12-30"), "month", Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_RANGE_INVALID, result.ErrorCode);
        }

        [Fact]
        public void Create_DayGroupingOfMoreThan92DaysAndFromTooOld_FailsWithDateTooOldFirst()
        {
            Result<StatsPeriod> result = StatsPeriod.Create(DateOf("2025-12-31"), Today, "day", Today, null);

            Assert.Equal(ResultCodes.ADMIN_STATS_DATE_TOO_OLD, result.ErrorCode);
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
