using System.Text.Json;
using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public class AdminStatsBucketTests(AdminStatsApp app) : AdminStatsTestBase(app)
    {
        [Fact]
        public async Task Stats_ByDay_HasOneZeroFilledBucketPerDayAndCountsEachDay()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-01T08:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-01T09:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-03T09:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04", by: "day");

            Assert.Equal(["2026-11-01", "2026-11-02", "2026-11-03", "2026-11-04"], StatsJson.Buckets(stats));
            Assert.Equal([2, 0, 1, 0], StatsJson.Ints(stats, "signUps", "counts"));
            Assert.Equal(3, StatsJson.Int(stats, "signUps", "total"));
        }

        [Fact]
        public async Task Stats_ByDay_EventsOutsideTheRangeNeverCount()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-31T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-03T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-02");

            Assert.Equal([0, 1], StatsJson.Ints(stats, "signUps", "counts"));
            Assert.Equal(1, StatsJson.Int(stats, "signUps", "total"));
        }

        [Fact]
        public async Task Stats_ByWeek_BucketStartsOnMondayAndASundayBelongsToTheWeekBefore()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-30T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-01T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-04T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04", by: "week");

            Assert.Equal(["2026-10-26", "2026-11-02"], StatsJson.Buckets(stats));
            Assert.Equal([1, 2], StatsJson.Ints(stats, "signUps", "counts"));
            Assert.Equal(3, StatsJson.Int(stats, "signUps", "total"));
        }

        [Fact]
        public async Task Stats_ByWeek_EmptyWeeksInTheMiddleAreZeroFilled()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.GroupCreated(person, Moment.At("2026-10-14T10:00:00Z")),
                UsageEvent.GroupCreated(person, Moment.At("2026-11-03T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-10-12", to: "2026-11-04", by: "week");

            Assert.Equal(["2026-10-12", "2026-10-19", "2026-10-26", "2026-11-02"], StatsJson.Buckets(stats));
            Assert.Equal([1, 0, 0, 1], StatsJson.Ints(stats, "groupsCreated", "counts"));
        }

        [Fact]
        public async Task Stats_ByMonth_BucketStartsOnTheFirstAndEventsBeforeFromInTheFirstMonthDoNotCount()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.ExpenseAdded(person, Moment.At("2026-10-14T10:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2026-10-15T10:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2026-10-31T10:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2026-11-04T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-10-15", to: "2026-11-04", by: "month");

            Assert.Equal(["2026-10-01", "2026-11-01"], StatsJson.Buckets(stats));
            Assert.Equal([2, 1], StatsJson.Ints(stats, "expensesAdded", "counts"));
            Assert.Equal(3, StatsJson.Int(stats, "expensesAdded", "total"));
        }

        [Fact]
        public async Task Stats_ByMonth_EmptyMonthsInTheMiddleAreZeroFilled()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SettlementConfirmed(person, Moment.At("2026-08-20T10:00:00Z")),
                UsageEvent.SettlementConfirmed(person, Moment.At("2026-11-02T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-08-15", to: "2026-11-04", by: "month");

            Assert.Equal(["2026-08-01", "2026-09-01", "2026-10-01", "2026-11-01"], StatsJson.Buckets(stats));
            Assert.Equal([1, 0, 0, 1], StatsJson.Ints(stats, "paymentsConfirmed", "counts"));
        }

        [Fact]
        public async Task Stats_ByYear_BucketStartsOn1JanuaryAndEventsBeforeFromDoNotCount()
        {
            _app.Clock.SetTo(Moment.At("2027-11-04T12:00:00Z"));
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.JoinedViaInvite(person, Moment.At("2026-10-31T10:00:00Z")),
                UsageEvent.JoinedViaInvite(person, Moment.At("2026-12-15T10:00:00Z")),
                UsageEvent.JoinedViaInvite(person, Moment.At("2027-06-01T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2027-11-04", by: "year");

            Assert.Equal(["2026-01-01", "2027-01-01"], StatsJson.Buckets(stats));
            Assert.Equal([1, 1], StatsJson.Ints(stats, "joinedViaInvite", "counts"));
            Assert.Equal(2, StatsJson.Int(stats, "joinedViaInvite", "total"));
        }

        [Fact]
        public async Task Stats_ByYear_NewYearsEveInSkopjeFallsInTheNextYear()
        {
            _app.Clock.SetTo(Moment.At("2027-02-01T12:00:00Z"));
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-12-31T22:59:59Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-12-31T23:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-12-01", to: "2027-01-31", by: "year");

            Assert.Equal(["2026-01-01", "2027-01-01"], StatsJson.Buckets(stats));
            Assert.Equal([1, 1], StatsJson.Ints(stats, "signUps", "counts"));
        }

        [Fact]
        public async Task Stats_ByMonth_LastNightOfTheMonthInSkopjeFallsInTheNextMonth()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-31T22:59:59Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-31T23:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-10-01", to: "2026-11-04", by: "month");

            Assert.Equal([1, 1], StatsJson.Ints(stats, "signUps", "counts"));
        }

        [Fact]
        public async Task Stats_ByWeek_SundayNightInSkopjeFallsInTheNextWeek()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-01T22:59:59Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-01T23:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-10-26", to: "2026-11-04", by: "week");

            Assert.Equal(["2026-10-26", "2026-11-02"], StatsJson.Buckets(stats));
            Assert.Equal([1, 1], StatsJson.Ints(stats, "signUps", "counts"));
        }
    }
}
