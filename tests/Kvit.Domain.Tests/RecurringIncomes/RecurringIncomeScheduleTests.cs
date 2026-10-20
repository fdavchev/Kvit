using System.Globalization;
using Kvit.Domain.Entities;
using Kvit.Domain.RecurringIncomes;
using Xunit;

namespace Kvit.Domain.Tests.RecurringIncomes
{
    public class RecurringIncomeScheduleTests
    {
        public static TheoryData<int, string[]> WeekdaysOfOctober2026 => new()
        {
            { 0, ["2026-10-04", "2026-10-11", "2026-10-18", "2026-10-25"] },
            { 1, ["2026-10-05", "2026-10-12", "2026-10-19", "2026-10-26"] },
            { 2, ["2026-10-06", "2026-10-13", "2026-10-20", "2026-10-27"] },
            { 3, ["2026-10-07", "2026-10-14", "2026-10-21", "2026-10-28"] },
            { 4, ["2026-10-01", "2026-10-08", "2026-10-15", "2026-10-22", "2026-10-29"] },
            { 5, ["2026-10-02", "2026-10-09", "2026-10-16", "2026-10-23", "2026-10-30"] },
            { 6, ["2026-10-03", "2026-10-10", "2026-10-17", "2026-10-24", "2026-10-31"] },
        };

        [Fact]
        public void Monthly_DayInsideTheRange_ReturnsThatDayOfEachMonth()
        {
            List<DateOnly> dates = MonthlyDates(15, "2026-10-01", "2026-12-31");

            Assert.Equal(DatesOf("2026-10-15", "2026-11-15", "2026-12-15"), dates);
        }

        [Fact]
        public void Monthly_RangeStartingAndEndingOnDueDates_IncludesBothEnds()
        {
            List<DateOnly> dates = MonthlyDates(15, "2026-10-15", "2026-12-15");

            Assert.Equal(DatesOf("2026-10-15", "2026-11-15", "2026-12-15"), dates);
        }

        [Fact]
        public void Monthly_RangeStartingOneDayAfterADueDateAndEndingOneDayBeforeAnother_LeavesBothOut()
        {
            List<DateOnly> dates = MonthlyDates(15, "2026-10-16", "2026-12-14");

            Assert.Equal(DatesOf("2026-11-15"), dates);
        }

        [Fact]
        public void Monthly_FromAfterTo_ReturnsNothing()
        {
            List<DateOnly> dates = MonthlyDates(15, "2026-12-31", "2026-10-01");

            Assert.Empty(dates);
        }

        [Fact]
        public void Monthly_FromOneDayAfterToOnTheDueDate_ReturnsNothing()
        {
            List<DateOnly> dates = MonthlyDates(15, "2026-10-16", "2026-10-15");

            Assert.Empty(dates);
        }

        [Fact]
        public void Monthly_OneDayRangeOnTheDueDay_ReturnsThatDay()
        {
            List<DateOnly> dates = MonthlyDates(15, "2026-11-15", "2026-11-15");

            Assert.Equal(DatesOf("2026-11-15"), dates);
        }

        [Theory]
        [InlineData("2026-11-14")]
        [InlineData("2026-11-16")]
        public void Monthly_OneDayRangeOffTheDueDay_ReturnsNothing(string day)
        {
            List<DateOnly> dates = MonthlyDates(15, day, day);

            Assert.Empty(dates);
        }

        [Fact]
        public void Monthly_RangeInsideOneMonthThatMissesTheDay_ReturnsNothing()
        {
            List<DateOnly> dates = MonthlyDates(15, "2026-10-16", "2026-10-31");

            Assert.Empty(dates);
        }

        [Fact]
        public void Monthly_RangeAcrossTheYearBoundary_ReturnsDecemberAndJanuary()
        {
            List<DateOnly> dates = MonthlyDates(15, "2026-12-10", "2027-01-20");

            Assert.Equal(DatesOf("2026-12-15", "2027-01-15"), dates);
        }

        [Fact]
        public void Monthly_RangeAcrossTheYearBoundaryThatMissesBothDueDays_ReturnsNothing()
        {
            List<DateOnly> dates = MonthlyDates(15, "2026-12-16", "2027-01-14");

            Assert.Empty(dates);
        }

        [Fact]
        public void Monthly_DayOne_FallsOnTheFirstOfEveryMonthOfTheRange()
        {
            List<DateOnly> dates = MonthlyDates(1, "2026-11-15", "2027-03-01");

            Assert.Equal(DatesOf("2026-12-01", "2027-01-01", "2027-02-01", "2027-03-01"), dates);
        }

        [Fact]
        public void Monthly_Day31AcrossAWholeYear_FallsOnTheLastDayOfShortMonths()
        {
            List<DateOnly> dates = MonthlyDates(31, "2026-01-01", "2026-12-31");

            Assert.Equal(
                DatesOf("2026-01-31", "2026-02-28", "2026-03-31", "2026-04-30", "2026-05-31", "2026-06-30", "2026-07-31", "2026-08-31", "2026-09-30", "2026-10-31", "2026-11-30", "2026-12-31"),
                dates);
        }

        [Fact]
        public void Monthly_Day30AcrossAWholeYear_OnlyFebruaryFallsEarly()
        {
            List<DateOnly> dates = MonthlyDates(30, "2026-01-01", "2026-12-31");

            Assert.Equal(
                DatesOf("2026-01-30", "2026-02-28", "2026-03-30", "2026-04-30", "2026-05-30", "2026-06-30", "2026-07-30", "2026-08-30", "2026-09-30", "2026-10-30", "2026-11-30", "2026-12-30"),
                dates);
        }

        [Fact]
        public void Monthly_Day29AcrossAWholeYear_OnlyFebruaryFallsEarly()
        {
            List<DateOnly> dates = MonthlyDates(29, "2026-01-01", "2026-12-31");

            Assert.Equal(
                DatesOf("2026-01-29", "2026-02-28", "2026-03-29", "2026-04-29", "2026-05-29", "2026-06-29", "2026-07-29", "2026-08-29", "2026-09-29", "2026-10-29", "2026-11-29", "2026-12-29"),
                dates);
        }

        [Fact]
        public void Monthly_Day29AcrossALeapYear_FebruaryKeepsItsOwn29th()
        {
            List<DateOnly> dates = MonthlyDates(29, "2028-01-01", "2028-03-31");

            Assert.Equal(DatesOf("2028-01-29", "2028-02-29", "2028-03-29"), dates);
        }

        [Theory]
        [InlineData(2026, 31, "2026-02-28")]
        [InlineData(2026, 30, "2026-02-28")]
        [InlineData(2026, 29, "2026-02-28")]
        [InlineData(2026, 28, "2026-02-28")]
        [InlineData(2028, 31, "2028-02-29")]
        [InlineData(2028, 30, "2028-02-29")]
        [InlineData(2028, 29, "2028-02-29")]
        [InlineData(2028, 28, "2028-02-28")]
        [InlineData(2100, 29, "2100-02-28")]
        [InlineData(2000, 30, "2000-02-29")]
        public void Monthly_DayInFebruary_FallsOnTheDayOrTheLastDayOfFebruary(int year, int dayOfMonth, string expected)
        {
            List<DateOnly> dates = MonthlyDates(dayOfMonth, $"{year}-02-01", $"{year}-02-{DateTime.DaysInMonth(year, 2)}");

            Assert.Equal(DatesOf(expected), dates);
        }

        [Theory]
        [InlineData(4, "2026-04-30")]
        [InlineData(6, "2026-06-30")]
        [InlineData(9, "2026-09-30")]
        [InlineData(11, "2026-11-30")]
        public void Monthly_Day31InA30DayMonth_FallsOnThe30th(int month, string expected)
        {
            List<DateOnly> dates = MonthlyDates(31, $"2026-{month:00}-01", expected);

            Assert.Equal(DatesOf(expected), dates);
        }

        [Fact]
        public void Monthly_ClampedDayOutsideTheRange_IsNotReturned()
        {
            Assert.Empty(MonthlyDates(31, "2026-02-01", "2026-02-27"));
            Assert.Empty(MonthlyDates(31, "2026-03-01", "2026-03-30"));
            Assert.Empty(MonthlyDates(31, "2026-04-01", "2026-04-29"));
        }

        [Fact]
        public void Monthly_ClampedDayAtTheEdgesOfTheRange_IsReturned()
        {
            Assert.Equal(DatesOf("2026-02-28"), MonthlyDates(31, "2026-02-28", "2026-03-01"));
            Assert.Equal(DatesOf("2026-02-28"), MonthlyDates(31, "2026-02-20", "2026-02-28"));
            Assert.Equal(DatesOf("2026-04-30"), MonthlyDates(31, "2026-04-30", "2026-04-30"));
        }

        [Fact]
        public void Monthly_Day31ClampedInFebruaryAndMarch_DoesNotReturnFebruaryTwice()
        {
            List<DateOnly> dates = MonthlyDates(31, "2026-02-01", "2026-03-31");

            Assert.Equal(DatesOf("2026-02-28", "2026-03-31"), dates);
        }

        [Fact]
        public void Monthly_Day15Across24Months_ReturnsOneDatePerMonthInOrder()
        {
            List<DateOnly> dates = MonthlyDates(15, "2025-01-01", "2026-12-31");

            Assert.Equal(24, dates.Count);
            Assert.Equal(new DateOnly(2025, 1, 15), dates[0]);
            Assert.Equal(new DateOnly(2026, 12, 15), dates[^1]);
            Assert.All(dates, date => Assert.Equal(15, date.Day));
            Assert.Equal(dates.Order(), dates);
            Assert.Equal(24, dates.Select(date => (date.Year, date.Month)).Distinct().Count());
        }

        [Fact]
        public void Monthly_Day31Across24Months_EveryDateIsTheLastDayOfItsMonthOrThe31st()
        {
            List<DateOnly> dates = MonthlyDates(31, "2025-01-01", "2026-12-31");

            Assert.Equal(24, dates.Count);
            Assert.All(dates, date => Assert.Equal(Math.Min(31, DateTime.DaysInMonth(date.Year, date.Month)), date.Day));
            Assert.Equal(dates.Order(), dates);
            Assert.Equal(24, dates.Select(date => (date.Year, date.Month)).Distinct().Count());
        }

        [Fact]
        public void Monthly_Day31AcrossThreeYearsWithALeapYear_FebruaryHas28Then28Then29()
        {
            List<DateOnly> dates = MonthlyDates(31, "2026-01-01", "2028-12-31");

            Assert.Equal(36, dates.Count);
            Assert.Contains(new DateOnly(2026, 2, 28), dates);
            Assert.Contains(new DateOnly(2027, 2, 28), dates);
            Assert.Contains(new DateOnly(2028, 2, 29), dates);
            Assert.DoesNotContain(new DateOnly(2028, 2, 28), dates);
        }

        [Theory]
        [MemberData(nameof(WeekdaysOfOctober2026))]
        public void Weekly_EachWeekdayAcrossOctober2026_ReturnsEveryOccurrence(int dayOfWeek, string[] expected)
        {
            List<DateOnly> dates = WeeklyDates(dayOfWeek, "2026-10-01", "2026-10-31");

            Assert.Equal(DatesOf(expected), dates);
        }

        [Theory]
        [InlineData(0, "2026-10-04")]
        [InlineData(1, "2026-10-05")]
        [InlineData(2, "2026-10-06")]
        [InlineData(3, "2026-10-07")]
        [InlineData(4, "2026-10-08")]
        [InlineData(5, "2026-10-09")]
        [InlineData(6, "2026-10-10")]
        public void Weekly_OneSundayToSaturdayWeek_ReturnsExactlyTheChosenWeekdayWithZeroAsSunday(int dayOfWeek, string expected)
        {
            List<DateOnly> dates = WeeklyDates(dayOfWeek, "2026-10-04", "2026-10-10");

            Assert.Equal(DatesOf(expected), dates);
            Assert.Equal((DayOfWeek)dayOfWeek, Assert.Single(dates).DayOfWeek);
        }

        [Fact]
        public void Weekly_RangeAcrossAMonthChange_ReturnsDatesFromBothMonths()
        {
            List<DateOnly> dates = WeeklyDates(5, "2026-10-25", "2026-11-10");

            Assert.Equal(DatesOf("2026-10-30", "2026-11-06"), dates);
        }

        [Fact]
        public void Weekly_RangeAcrossTheYearBoundary_ReturnsDatesFromBothYears()
        {
            List<DateOnly> dates = WeeklyDates(4, "2026-12-20", "2027-01-10");

            Assert.Equal(DatesOf("2026-12-24", "2026-12-31", "2027-01-07"), dates);
        }

        [Fact]
        public void Weekly_RangeAcrossALeapDay_IncludesThe29thOfFebruary()
        {
            List<DateOnly> dates = WeeklyDates(2, "2028-02-22", "2028-03-07");

            Assert.Equal(DatesOf("2028-02-22", "2028-02-29", "2028-03-07"), dates);
        }

        [Fact]
        public void Weekly_FromAfterTo_ReturnsNothing()
        {
            List<DateOnly> dates = WeeklyDates(3, "2026-10-31", "2026-10-01");

            Assert.Empty(dates);
        }

        [Fact]
        public void Weekly_OneDayRangeOnTheChosenWeekday_ReturnsThatDay()
        {
            List<DateOnly> dates = WeeklyDates(3, "2026-10-07", "2026-10-07");

            Assert.Equal(DatesOf("2026-10-07"), dates);
        }

        [Theory]
        [InlineData("2026-10-06")]
        [InlineData("2026-10-08")]
        public void Weekly_OneDayRangeOnAnotherWeekday_ReturnsNothing(string day)
        {
            List<DateOnly> dates = WeeklyDates(3, day, day);

            Assert.Empty(dates);
        }

        [Fact]
        public void Weekly_RangeOfSixDaysThatMissesTheWeekday_ReturnsNothing()
        {
            List<DateOnly> dates = WeeklyDates(3, "2026-10-08", "2026-10-13");

            Assert.Empty(dates);
        }

        [Fact]
        public void Weekly_MondayAcross24Months_ReturnsEverySeventhDayInOrder()
        {
            List<DateOnly> dates = WeeklyDates(1, "2025-01-01", "2026-12-31");

            Assert.Equal(104, dates.Count);
            Assert.Equal(new DateOnly(2025, 1, 6), dates[0]);
            Assert.Equal(new DateOnly(2026, 12, 28), dates[^1]);
            Assert.All(dates, date => Assert.Equal(DayOfWeek.Monday, date.DayOfWeek));
            for (int index = 1; index < dates.Count; index++)
            {
                Assert.Equal(7, dates[index].DayNumber - dates[index - 1].DayNumber);
            }
        }

        [Fact]
        public void Weekly_WednesdayAcross2025_Returns53BecauseTheYearStartsAndEndsOnAWednesday()
        {
            List<DateOnly> dates = WeeklyDates(3, "2025-01-01", "2025-12-31");

            Assert.Equal(53, dates.Count);
        }

        private static List<DateOnly> MonthlyDates(int dayOfMonth, string from, string to)
        {
            return [.. RecurringIncomeSchedule.DueDates(RecurringIncomeFrequency.Monthly, dayOfMonth, null, DateOf(from), DateOf(to))];
        }

        private static List<DateOnly> WeeklyDates(int dayOfWeek, string from, string to)
        {
            return [.. RecurringIncomeSchedule.DueDates(RecurringIncomeFrequency.Weekly, null, dayOfWeek, DateOf(from), DateOf(to))];
        }

        private static List<DateOnly> DatesOf(params string[] isoDates)
        {
            return [.. isoDates.Select(DateOf)];
        }

        private static DateOnly DateOf(string isoDate)
        {
            return DateOnly.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
