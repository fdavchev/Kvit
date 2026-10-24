using System.Globalization;
using Kvit.Domain.Accounts;
using Xunit;

namespace Kvit.Domain.Tests.Accounts
{
    public class UserLocalDateTests
    {
        [Theory]
        [InlineData("2026-01-15T22:59:59Z", "2026-01-15")]
        [InlineData("2026-01-15T23:00:00Z", "2026-01-16")]
        [InlineData("2026-01-16T00:00:00Z", "2026-01-16")]
        public void Today_SkopjeInWinter_RollsOverAtTwentyThreeUtc(string utcNow, string expected)
        {
            DateOnly today = UserLocalDate.Today(InstantOf(utcNow), "Europe/Skopje");

            Assert.Equal(DateOf(expected), today);
        }

        [Theory]
        [InlineData("2026-07-15T21:59:59Z", "2026-07-15")]
        [InlineData("2026-07-15T22:00:00Z", "2026-07-16")]
        public void Today_SkopjeInSummer_RollsOverAtTwentyTwoUtc(string utcNow, string expected)
        {
            DateOnly today = UserLocalDate.Today(InstantOf(utcNow), "Europe/Skopje");

            Assert.Equal(DateOf(expected), today);
        }

        [Theory]
        [InlineData("2026-03-28T22:59:59Z", "2026-03-28")]
        [InlineData("2026-03-28T23:00:00Z", "2026-03-29")]
        [InlineData("2026-03-29T00:59:59Z", "2026-03-29")]
        [InlineData("2026-03-29T01:00:00Z", "2026-03-29")]
        [InlineData("2026-03-29T21:59:59Z", "2026-03-29")]
        [InlineData("2026-03-29T22:00:00Z", "2026-03-30")]
        public void Today_SkopjeOnTheDayClocksGoForward_FollowsTheChangeOfOffset(string utcNow, string expected)
        {
            DateOnly today = UserLocalDate.Today(InstantOf(utcNow), "Europe/Skopje");

            Assert.Equal(DateOf(expected), today);
        }

        [Theory]
        [InlineData("2026-10-24T21:59:59Z", "2026-10-24")]
        [InlineData("2026-10-24T22:00:00Z", "2026-10-25")]
        [InlineData("2026-10-25T00:59:59Z", "2026-10-25")]
        [InlineData("2026-10-25T01:00:00Z", "2026-10-25")]
        [InlineData("2026-10-25T22:59:59Z", "2026-10-25")]
        [InlineData("2026-10-25T23:00:00Z", "2026-10-26")]
        public void Today_SkopjeOnTheDayClocksGoBack_FollowsTheChangeOfOffset(string utcNow, string expected)
        {
            DateOnly today = UserLocalDate.Today(InstantOf(utcNow), "Europe/Skopje");

            Assert.Equal(DateOf(expected), today);
        }

        [Theory]
        [InlineData("2026-01-15T04:59:59Z", "2026-01-14")]
        [InlineData("2026-01-15T05:00:00Z", "2026-01-15")]
        [InlineData("2026-11-01T03:59:59Z", "2026-10-31")]
        [InlineData("2026-11-01T04:00:00Z", "2026-11-01")]
        public void Today_NewYork_IsFourHoursBehindInSummerAndFiveInWinter(string utcNow, string expected)
        {
            DateOnly today = UserLocalDate.Today(InstantOf(utcNow), "America/New_York");

            Assert.Equal(DateOf(expected), today);
        }

        [Theory]
        [InlineData("2026-11-01T09:59:59Z", "2026-10-31")]
        [InlineData("2026-11-01T10:00:00Z", "2026-11-01")]
        public void Today_Honolulu_IsTenHoursBehindAllYear(string utcNow, string expected)
        {
            DateOnly today = UserLocalDate.Today(InstantOf(utcNow), "Pacific/Honolulu");

            Assert.Equal(DateOf(expected), today);
        }

        [Theory]
        [InlineData("2026-10-31T09:59:59Z", "2026-10-31")]
        [InlineData("2026-10-31T10:00:00Z", "2026-11-01")]
        [InlineData("2026-10-31T10:30:00Z", "2026-11-01")]
        public void Today_Kiritimati_IsFourteenHoursAheadSoItIsAlreadyTomorrowInTheUtcMorning(string utcNow, string expected)
        {
            DateOnly today = UserLocalDate.Today(InstantOf(utcNow), "Pacific/Kiritimati");

            Assert.Equal(DateOf(expected), today);
        }

        [Theory]
        [InlineData("2026-12-31T23:59:59Z", "2026-12-31")]
        [InlineData("2027-01-01T00:00:00Z", "2027-01-01")]
        public void Today_Utc_IsTheUtcDate(string utcNow, string expected)
        {
            DateOnly today = UserLocalDate.Today(InstantOf(utcNow), "UTC");

            Assert.Equal(DateOf(expected), today);
        }

        [Fact]
        public void Today_InstantGivenWithAnotherOffset_UsesTheInstantNotTheWrittenClock()
        {
            DateTimeOffset fivePastMidnightInTheEast = new(2026, 11, 1, 3, 0, 0, TimeSpan.FromHours(5));

            DateOnly utcToday = UserLocalDate.Today(fivePastMidnightInTheEast, "UTC");
            DateOnly honoluluToday = UserLocalDate.Today(fivePastMidnightInTheEast, "Pacific/Honolulu");

            Assert.Equal(new DateOnly(2026, 10, 31), utcToday);
            Assert.Equal(new DateOnly(2026, 10, 31), honoluluToday);
        }

        [Fact]
        public void Today_SkopjeOnTheLastEveningOfFebruaryInALeapYear_RollsOverToTheLeapDay()
        {
            DateOnly today = UserLocalDate.Today(InstantOf("2028-02-28T23:30:00Z"), "Europe/Skopje");

            Assert.Equal(new DateOnly(2028, 2, 29), today);
        }

        [Fact]
        public void Today_SkopjeOnNewYearsEve_RollsOverIntoTheNextYear()
        {
            DateOnly today = UserLocalDate.Today(InstantOf("2026-12-31T23:00:00Z"), "Europe/Skopje");

            Assert.Equal(new DateOnly(2027, 1, 1), today);
        }

        [Theory]
        [InlineData("Mars/Olympus_Mons")]
        [InlineData("Skopje")]
        [InlineData("Europe/Atlantis")]
        public void Today_UnknownTimeZoneId_ThrowsInvalidOperationExceptionNamingTheId(string timeZoneId)
        {
            DateTimeOffset utcNow = InstantOf("2026-10-20T12:00:00Z");

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => UserLocalDate.Today(utcNow, timeZoneId));

            Assert.Contains(timeZoneId, error.Message);
        }

        private static DateTimeOffset InstantOf(string utcInstant)
        {
            return DateTimeOffset.Parse(utcInstant, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        }

        private static DateOnly DateOf(string isoDate)
        {
            return DateOnly.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
