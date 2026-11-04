using System.Text.Json;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public class AdminStatsRangeTests(AdminStatsApp app) : AdminStatsTestBase(app)
    {
        private static readonly string[] Sections = ["signUps", "joinedViaInvite", "groupsCreated", "expensesAdded", "paymentsConfirmed"];

        [Fact]
        public async Task Stats_NoParameters_UsesDayGroupingAndTodayInSkopje()
        {
            JsonElement stats = await StatsAsync();

            Assert.Equal("day", StatsJson.Text(stats, "by"));
            Assert.Equal("2026-11-04", StatsJson.Text(stats, "to"));
        }

        [Fact]
        public async Task Stats_ToOmittedShortlyAfterMidnightInSkopje_UsesTheSkopjeDayNotTheUtcDay()
        {
            _app.Clock.SetTo(Moment.At("2026-11-03T23:30:00Z"));

            JsonElement stats = await StatsAsync(from: "2026-11-03");

            Assert.Equal("2026-11-04", StatsJson.Text(stats, "to"));
            Assert.Equal(["2026-11-03", "2026-11-04"], StatsJson.Buckets(stats));
        }

        [Fact]
        public async Task Stats_ToEqualToTodayInSkopjeWhileTheUtcDateIsYesterday_Answers200()
        {
            _app.Clock.SetTo(Moment.At("2026-11-03T23:30:00Z"));

            JsonElement stats = await StatsAsync(from: "2026-11-04", to: "2026-11-04");

            Assert.Equal(["2026-11-04"], StatsJson.Buckets(stats));
        }

        [Fact]
        public async Task Stats_ByInMixedCase_AnswersTheLowercaseGrouping()
        {
            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04", by: "WeEk");

            Assert.Equal("week", StatsJson.Text(stats, "by"));
        }

        [Fact]
        public async Task Stats_ExplicitRange_AnswersTheRangeAndThePeriodOfTheSameLengthBefore()
        {
            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            Assert.Equal("2026-11-01", StatsJson.Text(stats, "from"));
            Assert.Equal("2026-11-04", StatsJson.Text(stats, "to"));
            Assert.Equal("2026-10-28", StatsJson.Text(stats, "previousFrom"));
            Assert.Equal("2026-10-31", StatsJson.Text(stats, "previousTo"));
        }

        [Fact]
        public async Task Stats_SingleDay_PreviousPeriodIsTheDayBefore()
        {
            JsonElement stats = await StatsAsync(from: "2026-11-04", to: "2026-11-04");

            Assert.Equal("2026-11-03", StatsJson.Text(stats, "previousFrom"));
            Assert.Equal("2026-11-03", StatsJson.Text(stats, "previousTo"));
        }

        [Fact]
        public async Task Stats_ThirtyOneDayRange_PreviousPeriodHasThirtyOneDaysAcrossMonthEnds()
        {
            JsonElement stats = await StatsAsync(from: "2026-10-01", to: "2026-10-31", by: "week");

            Assert.Equal("2026-08-31", StatsJson.Text(stats, "previousFrom"));
            Assert.Equal("2026-09-30", StatsJson.Text(stats, "previousTo"));
        }

        [Fact]
        public async Task Stats_PreviousPeriod_CountsOnlyEventsInsideThePreviousRange()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-27T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-28T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Google, Moment.At("2026-10-31T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Guest, Moment.At("2026-11-01T10:00:00Z")),
                UsageEvent.JoinedViaInvite(person, Moment.At("2026-10-29T10:00:00Z")),
                UsageEvent.GroupCreated(person, Moment.At("2026-10-29T10:00:00Z")),
                UsageEvent.GroupCreated(person, Moment.At("2026-10-30T10:00:00Z")),
                UsageEvent.GroupCreated(person, Moment.At("2026-10-30T11:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2026-10-28T10:00:00Z")),
                UsageEvent.SettlementConfirmed(person, Moment.At("2026-10-30T10:00:00Z")),
                UsageEvent.SettlementConfirmed(person, Moment.At("2026-10-31T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(2, StatsJson.Int(stats, "signUps", "previousTotal"));
            Assert.Equal(1, StatsJson.Int(stats, "joinedViaInvite", "previousTotal"));
            Assert.Equal(3, StatsJson.Int(stats, "groupsCreated", "previousTotal"));
            Assert.Equal(1, StatsJson.Int(stats, "expensesAdded", "previousTotal"));
            Assert.Equal(2, StatsJson.Int(stats, "paymentsConfirmed", "previousTotal"));
        }

        [Fact]
        public async Task Stats_FromOmitted_StartsAtTheSkopjeDayOfTheEarliestEvent()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-31T23:30:00Z")),
                UsageEvent.GroupCreated(person, Moment.At("2026-11-03T10:00:00Z")));

            JsonElement stats = await StatsAsync();

            Assert.Equal("2026-11-01", StatsJson.Text(stats, "from"));
            Assert.Equal("2026-11-04", StatsJson.Text(stats, "to"));
            Assert.Equal(["2026-11-01", "2026-11-02", "2026-11-03", "2026-11-04"], StatsJson.Buckets(stats));
            Assert.Equal(1, StatsJson.Int(stats, "signUps", "total"));
        }

        [Fact]
        public async Task Stats_FromOmittedAndAnAdminHasTheEarliestEvent_IgnoresTheAdminEvent()
        {
            Guid person = await _app.AddPersonAsync();
            SignedInUser admin = await _app.SignInAdminAsync();
            await _app.AddEventsAsync(
                UsageEvent.GroupCreated(admin.UserId, Moment.At("2026-10-20T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-02T10:00:00Z")));

            JsonElement stats = await AdminStatsRequests.ReadAsync(admin.Client);

            Assert.Equal("2026-11-02", StatsJson.Text(stats, "from"));
        }

        [Fact]
        public async Task Stats_FromOmitted_HasNoPreviousPeriodAndNoPreviousValues()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-01T10:00:00Z")),
                UsageEvent.Active(person, Moment.At("2026-11-02T10:00:00Z")));

            JsonElement stats = await StatsAsync();

            Assert.True(StatsJson.IsNull(stats, "previousFrom"));
            Assert.True(StatsJson.IsNull(stats, "previousTo"));
            foreach (string section in Sections)
            {
                Assert.True(StatsJson.IsNull(stats, section, "previousTotal"), $"{section}.previousTotal should be null");
            }

            Assert.True(StatsJson.IsNull(stats, "activePeople", "previousPeople"));
            Assert.True(StatsJson.IsNull(stats, "activePeople", "previousPersonDays"));
        }

        [Fact]
        public async Task Stats_FromGiven_PreviousValuesAreNumbersEvenWhenThereIsNoData()
        {
            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            foreach (string section in Sections)
            {
                Assert.Equal(0, StatsJson.Int(stats, section, "previousTotal"));
            }

            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "previousPeople"));
            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "previousPersonDays"));
        }

        [Fact]
        public async Task Stats_EmptyDatabaseAndFromOmitted_UsesTodayAsTheOnlyDayWithZeros()
        {
            JsonElement stats = await StatsAsync();

            Assert.Equal("2026-11-04", StatsJson.Text(stats, "from"));
            Assert.Equal("2026-11-04", StatsJson.Text(stats, "to"));
            Assert.Equal(["2026-11-04"], StatsJson.Buckets(stats));
            Assert.Equal([0], StatsJson.Ints(stats, "signUps", "counts"));
            Assert.Equal([0], StatsJson.Ints(stats, "activePeople", "peopleCounts"));
            Assert.Equal([0], StatsJson.Ints(stats, "activePeople", "personDayCounts"));
        }

        [Fact]
        public async Task Stats_EmptyDatabase_AnswersZerosEverywhereWithOneCountPerBucket()
        {
            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            foreach (string section in Sections)
            {
                Assert.Equal(0, StatsJson.Int(stats, section, "total"));
                Assert.Equal([0, 0, 0, 0], StatsJson.Ints(stats, section, "counts"));
            }

            Assert.Equal(0, StatsJson.Int(stats, "signUps", "email"));
            Assert.Equal(0, StatsJson.Int(stats, "signUps", "google"));
            Assert.Equal(0, StatsJson.Int(stats, "signUps", "guest"));
            Assert.Equal(0, StatsJson.Int(stats, "signUps", "guestsUpgraded"));
            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "people"));
            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "personDays"));
            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "guests"));
            Assert.Equal([0, 0, 0, 0], StatsJson.Ints(stats, "activePeople", "peopleCounts"));
            Assert.Equal([0, 0, 0, 0], StatsJson.Ints(stats, "activePeople", "personDayCounts"));
            Assert.Equal(0, StatsJson.Int(stats, "allTime", "accounts"));
            Assert.Equal(0, StatsJson.Int(stats, "allTime", "guests"));
            Assert.Equal(0, StatsJson.Int(stats, "allTime", "groups"));
            Assert.Equal(0, StatsJson.Int(stats, "allTime", "expenses"));
            Assert.Equal(0, StatsJson.Int(stats, "allTime", "payments"));
        }
    }
}
