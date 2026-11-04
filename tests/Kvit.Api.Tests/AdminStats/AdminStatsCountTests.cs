using System.Text.Json;
using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public class AdminStatsCountTests(AdminStatsApp app) : AdminStatsTestBase(app)
    {
        [Fact]
        public async Task Stats_SignUps_SplitByMethodAndGuestsUpgradedAreNotPartOfTheTotal()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-10-31T10:00:00Z")),
                UsageEvent.GuestUpgraded(person, SignUpMethod.Email, Moment.At("2026-10-31T11:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-01T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-01T11:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Google, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.GuestUpgraded(person, SignUpMethod.Email, Moment.At("2026-11-03T10:00:00Z")),
                UsageEvent.GuestUpgraded(person, SignUpMethod.Google, Moment.At("2026-11-04T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Guest, Moment.At("2026-11-04T08:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Guest, Moment.At("2026-11-04T09:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Guest, Moment.At("2026-11-04T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(6, StatsJson.Int(stats, "signUps", "total"));
            Assert.Equal(2, StatsJson.Int(stats, "signUps", "email"));
            Assert.Equal(1, StatsJson.Int(stats, "signUps", "google"));
            Assert.Equal(3, StatsJson.Int(stats, "signUps", "guest"));
            Assert.Equal(2, StatsJson.Int(stats, "signUps", "guestsUpgraded"));
            Assert.Equal([2, 1, 0, 3], StatsJson.Ints(stats, "signUps", "counts"));
        }

        [Fact]
        public async Task Stats_EachMetric_CountsOnlyItsOwnEventTypePerDay()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-01T06:00:00Z")),
                UsageEvent.Active(person, Moment.At("2026-11-02T06:00:00Z")),
                UsageEvent.GuestUpgraded(person, SignUpMethod.Email, Moment.At("2026-11-02T07:00:00Z")),
                UsageEvent.JoinedViaInvite(person, Moment.At("2026-11-01T10:00:00Z")),
                UsageEvent.GroupCreated(person, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.GroupCreated(person, Moment.At("2026-11-02T11:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2026-11-03T10:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2026-11-03T11:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2026-11-03T12:00:00Z")),
                UsageEvent.SettlementConfirmed(person, Moment.At("2026-11-04T08:00:00Z")),
                UsageEvent.SettlementConfirmed(person, Moment.At("2026-11-04T09:00:00Z")),
                UsageEvent.SettlementConfirmed(person, Moment.At("2026-11-04T10:00:00Z")),
                UsageEvent.SettlementConfirmed(person, Moment.At("2026-11-04T11:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(1, StatsJson.Int(stats, "signUps", "total"));
            Assert.Equal(1, StatsJson.Int(stats, "joinedViaInvite", "total"));
            Assert.Equal([1, 0, 0, 0], StatsJson.Ints(stats, "joinedViaInvite", "counts"));
            Assert.Equal(2, StatsJson.Int(stats, "groupsCreated", "total"));
            Assert.Equal([0, 2, 0, 0], StatsJson.Ints(stats, "groupsCreated", "counts"));
            Assert.Equal(3, StatsJson.Int(stats, "expensesAdded", "total"));
            Assert.Equal([0, 0, 3, 0], StatsJson.Ints(stats, "expensesAdded", "counts"));
            Assert.Equal(4, StatsJson.Int(stats, "paymentsConfirmed", "total"));
            Assert.Equal([0, 0, 0, 4], StatsJson.Ints(stats, "paymentsConfirmed", "counts"));
        }

        [Fact]
        public async Task Stats_AllTime_CountsEverythingThatEverHappenedWhateverTheRange()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2025-01-10T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2025-01-11T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Google, Moment.At("2025-01-12T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Guest, Moment.At("2025-01-13T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Guest, Moment.At("2025-01-14T10:00:00Z")),
                UsageEvent.GroupCreated(person, Moment.At("2025-02-01T10:00:00Z")),
                UsageEvent.GroupCreated(person, Moment.At("2025-02-02T10:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2025-03-01T10:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2025-03-02T10:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2025-03-03T10:00:00Z")),
                UsageEvent.SettlementConfirmed(person, Moment.At("2025-04-01T10:00:00Z")),
                UsageEvent.GuestUpgraded(person, SignUpMethod.Email, Moment.At("2025-05-01T10:00:00Z")),
                UsageEvent.JoinedViaInvite(person, Moment.At("2025-05-02T10:00:00Z")),
                UsageEvent.Active(person, Moment.At("2025-05-03T10:00:00Z")),
                UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-04T10:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2026-11-04T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-04", to: "2026-11-04");

            Assert.Equal(4, StatsJson.Int(stats, "allTime", "accounts"));
            Assert.Equal(2, StatsJson.Int(stats, "allTime", "guests"));
            Assert.Equal(2, StatsJson.Int(stats, "allTime", "groups"));
            Assert.Equal(4, StatsJson.Int(stats, "allTime", "expenses"));
            Assert.Equal(1, StatsJson.Int(stats, "allTime", "payments"));
        }
    }
}
