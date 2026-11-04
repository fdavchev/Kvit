using System.Text.Json;
using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public class AdminStatsSkopjeDayTests(AdminStatsApp app) : AdminStatsTestBase(app)
    {
        [Theory]
        [InlineData("2026-11-03T22:59:59Z", "2026-11-03")]
        [InlineData("2026-11-03T23:00:00Z", "2026-11-04")]
        [InlineData("2026-11-03T23:30:00Z", "2026-11-04")]
        [InlineData("2026-11-04T22:59:59Z", "2026-11-04")]
        [InlineData("2026-11-04T23:00:00Z", "2026-11-05")]
        [InlineData("2026-07-31T21:59:59Z", "2026-07-31")]
        [InlineData("2026-07-31T22:00:00Z", "2026-08-01")]
        [InlineData("2026-07-31T22:30:00Z", "2026-08-01")]
        public async Task Stats_EventNearSkopjeMidnight_CountsOnTheSkopjeDay(string occurredAt, string expectedDay)
        {
            _app.Clock.SetTo(Moment.At("2026-12-31T12:00:00Z"));
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At(occurredAt)));

            JsonElement stats = await StatsAsync(from: expectedDay, to: expectedDay);

            Assert.Equal(1, StatsJson.Int(stats, "signUps", "total"));
            Assert.Equal([1], StatsJson.Ints(stats, "signUps", "counts"));
        }

        [Fact]
        public async Task Stats_RangeAcrossTheAutumnSwitchBackToWinterTime_HasOneBucketPerCalendarDay()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-24T21:59:59Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-24T22:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-25T22:59:59Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-25T23:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-10-24", to: "2026-10-26");

            Assert.Equal(["2026-10-24", "2026-10-25", "2026-10-26"], StatsJson.Buckets(stats));
            Assert.Equal([1, 2, 1], StatsJson.Ints(stats, "signUps", "counts"));
        }

        [Fact]
        public async Task Stats_RangeAcrossTheSpringSwitchToSummerTime_HasOneBucketPerCalendarDay()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-03-28T22:59:59Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-03-28T23:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-03-29T21:59:59Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-03-29T22:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-03-28", to: "2026-03-30");

            Assert.Equal(["2026-03-28", "2026-03-29", "2026-03-30"], StatsJson.Buckets(stats));
            Assert.Equal([1, 2, 1], StatsJson.Ints(stats, "signUps", "counts"));
        }

        [Fact]
        public async Task Stats_TwoActiveRowsOfOnePersonOnTwoUtcDaysThatAreOneSkopjeDay_CountAsOnePersonDay()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.Active(person, Moment.At("2026-11-03T23:30:00Z")),
                UsageEvent.Active(person, Moment.At("2026-11-04T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-03", to: "2026-11-04");

            Assert.Equal(1, StatsJson.Int(stats, "activePeople", "people"));
            Assert.Equal(1, StatsJson.Int(stats, "activePeople", "personDays"));
            Assert.Equal([0, 1], StatsJson.Ints(stats, "activePeople", "peopleCounts"));
            Assert.Equal([0, 1], StatsJson.Ints(stats, "activePeople", "personDayCounts"));
        }
    }
}
