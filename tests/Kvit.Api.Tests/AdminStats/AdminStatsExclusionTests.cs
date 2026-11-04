using System.Text.Json;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public class AdminStatsExclusionTests(AdminStatsApp app) : AdminStatsTestBase(app)
    {
        [Fact]
        public async Task Stats_EventsOfEveryAdminAccount_AreLeftOutOfEveryNumber()
        {
            SignedInUser firstAdmin = await _app.SignInAdminAsync();
            SignedInUser secondAdmin = await _app.SignInAdminAsync();
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(AdminEventsOf(firstAdmin.UserId));
            await _app.AddEventsAsync(AdminEventsOf(secondAdmin.UserId));
            await _app.AddEventsAsync(UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-11-03T10:00:00Z")));

            JsonElement stats = await AdminStatsRequests.ReadAsync(firstAdmin.Client, from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(1, StatsJson.Int(stats, "signUps", "total"));
            Assert.Equal(1, StatsJson.Int(stats, "signUps", "email"));
            Assert.Equal(0, StatsJson.Int(stats, "signUps", "google"));
            Assert.Equal(0, StatsJson.Int(stats, "signUps", "guest"));
            Assert.Equal(0, StatsJson.Int(stats, "signUps", "guestsUpgraded"));
            Assert.Equal(0, StatsJson.Int(stats, "joinedViaInvite", "total"));
            Assert.Equal(0, StatsJson.Int(stats, "groupsCreated", "total"));
            Assert.Equal(0, StatsJson.Int(stats, "expensesAdded", "total"));
            Assert.Equal(0, StatsJson.Int(stats, "paymentsConfirmed", "total"));
            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "people"));
            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "personDays"));
        }

        [Fact]
        public async Task Stats_EventsOfAnAdminInThePreviousPeriod_AreLeftOut()
        {
            SignedInUser admin = await _app.SignInAdminAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(admin.UserId, SignUpMethod.Email, Moment.At("2026-10-29T10:00:00Z")),
                UsageEvent.GroupCreated(admin.UserId, Moment.At("2026-10-29T10:00:00Z")),
                UsageEvent.Active(admin.UserId, Moment.At("2026-10-29T10:00:00Z")));

            JsonElement stats = await AdminStatsRequests.ReadAsync(admin.Client, from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(0, StatsJson.Int(stats, "signUps", "previousTotal"));
            Assert.Equal(0, StatsJson.Int(stats, "groupsCreated", "previousTotal"));
            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "previousPeople"));
            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "previousPersonDays"));
        }

        [Fact]
        public async Task Stats_AllTimeTotals_LeaveOutAdminEvents()
        {
            SignedInUser admin = await _app.SignInAdminAsync();
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(admin.UserId, SignUpMethod.Email, Moment.At("2025-01-01T10:00:00Z")),
                UsageEvent.SignedUp(admin.UserId, SignUpMethod.Guest, Moment.At("2025-01-01T11:00:00Z")),
                UsageEvent.GroupCreated(admin.UserId, Moment.At("2025-01-02T10:00:00Z")),
                UsageEvent.ExpenseAdded(admin.UserId, Moment.At("2025-01-03T10:00:00Z")),
                UsageEvent.SettlementConfirmed(admin.UserId, Moment.At("2025-01-04T10:00:00Z")),
                UsageEvent.ExpenseAdded(person, Moment.At("2025-01-05T10:00:00Z")));

            JsonElement stats = await AdminStatsRequests.ReadAsync(admin.Client, from: "2026-11-04", to: "2026-11-04");

            Assert.Equal(0, StatsJson.Int(stats, "allTime", "accounts"));
            Assert.Equal(0, StatsJson.Int(stats, "allTime", "guests"));
            Assert.Equal(0, StatsJson.Int(stats, "allTime", "groups"));
            Assert.Equal(1, StatsJson.Int(stats, "allTime", "expenses"));
            Assert.Equal(0, StatsJson.Int(stats, "allTime", "payments"));
        }

        [Fact]
        public async Task Stats_ActivePeople_LeavesOutAnAdmin()
        {
            SignedInUser admin = await _app.SignInAdminAsync();
            Guid ana = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.Active(admin.UserId, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.Active(ana, Moment.At("2026-11-02T10:00:00Z")));

            JsonElement stats = await AdminStatsRequests.ReadAsync(admin.Client, from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(1, StatsJson.Int(stats, "activePeople", "people"));
            Assert.Equal([0, 1, 0, 0], StatsJson.Ints(stats, "activePeople", "peopleCounts"));
        }

        [Fact]
        public async Task Stats_EventsWithoutAUser_AreCountedEverywhere()
        {
            Guid deletedAccount = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(deletedAccount, SignUpMethod.Email, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.GroupCreated(deletedAccount, Moment.At("2026-11-02T11:00:00Z")),
                UsageEvent.ExpenseAdded(deletedAccount, Moment.At("2026-11-03T10:00:00Z")),
                UsageEvent.SettlementConfirmed(deletedAccount, Moment.At("2026-11-03T11:00:00Z")),
                UsageEvent.JoinedViaInvite(deletedAccount, Moment.At("2026-11-04T10:00:00Z")),
                UsageEvent.SignedUp(deletedAccount, SignUpMethod.Google, Moment.At("2026-10-30T10:00:00Z")));
            await _app.DeleteUserRowAsync(deletedAccount);
            Assert.Equal(6, await _app.CountUsageEventsWithoutUserAsync());

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(1, StatsJson.Int(stats, "signUps", "total"));
            Assert.Equal(1, StatsJson.Int(stats, "signUps", "previousTotal"));
            Assert.Equal(1, StatsJson.Int(stats, "groupsCreated", "total"));
            Assert.Equal(1, StatsJson.Int(stats, "expensesAdded", "total"));
            Assert.Equal(1, StatsJson.Int(stats, "paymentsConfirmed", "total"));
            Assert.Equal(1, StatsJson.Int(stats, "joinedViaInvite", "total"));
            Assert.Equal(2, StatsJson.Int(stats, "allTime", "accounts"));
            Assert.Equal(1, StatsJson.Int(stats, "allTime", "groups"));
            Assert.Equal(1, StatsJson.Int(stats, "allTime", "expenses"));
            Assert.Equal(1, StatsJson.Int(stats, "allTime", "payments"));
        }

        private static UsageEvent[] AdminEventsOf(Guid adminId)
        {
            return
            [
                UsageEvent.SignedUp(adminId, SignUpMethod.Email, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.SignedUp(adminId, SignUpMethod.Google, Moment.At("2026-11-02T11:00:00Z")),
                UsageEvent.SignedUp(adminId, SignUpMethod.Guest, Moment.At("2026-11-02T12:00:00Z")),
                UsageEvent.GuestUpgraded(adminId, SignUpMethod.Email, Moment.At("2026-11-02T13:00:00Z")),
                UsageEvent.JoinedViaInvite(adminId, Moment.At("2026-11-02T14:00:00Z")),
                UsageEvent.GroupCreated(adminId, Moment.At("2026-11-02T15:00:00Z")),
                UsageEvent.ExpenseAdded(adminId, Moment.At("2026-11-02T16:00:00Z")),
                UsageEvent.SettlementConfirmed(adminId, Moment.At("2026-11-02T17:00:00Z")),
                UsageEvent.Active(adminId, Moment.At("2026-11-02T18:00:00Z")),
            ];
        }
    }
}
