using System.Text.Json;
using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public class AdminStatsActivePeopleTests(AdminStatsApp app) : AdminStatsTestBase(app)
    {
        [Fact]
        public async Task Stats_ActivePeopleByDay_PeopleAreDistinctAndPersonDaysCountEachPersonEachDay()
        {
            Guid ana = await _app.AddPersonAsync();
            Guid bojan = await _app.AddPersonAsync();
            Guid guest = await _app.AddGuestAsync();
            await _app.AddEventsAsync(
                UsageEvent.Active(ana, Moment.At("2026-11-01T10:00:00Z")),
                UsageEvent.Active(ana, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.Active(ana, Moment.At("2026-11-04T10:00:00Z")),
                UsageEvent.Active(bojan, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.Active(guest, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.Active(guest, Moment.At("2026-11-03T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(3, StatsJson.Int(stats, "activePeople", "people"));
            Assert.Equal(6, StatsJson.Int(stats, "activePeople", "personDays"));
            Assert.Equal([1, 3, 1, 1], StatsJson.Ints(stats, "activePeople", "peopleCounts"));
            Assert.Equal([1, 3, 1, 1], StatsJson.Ints(stats, "activePeople", "personDayCounts"));
        }

        [Fact]
        public async Task Stats_ActivePeopleByWeek_EachBucketCountsDistinctPeopleAndPersonDaysInsideItself()
        {
            Guid ana = await _app.AddPersonAsync();
            Guid bojan = await _app.AddPersonAsync();
            Guid guest = await _app.AddGuestAsync();
            await _app.AddEventsAsync(
                UsageEvent.Active(ana, Moment.At("2026-11-01T10:00:00Z")),
                UsageEvent.Active(ana, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.Active(ana, Moment.At("2026-11-04T10:00:00Z")),
                UsageEvent.Active(bojan, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.Active(guest, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.Active(guest, Moment.At("2026-11-03T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04", by: "week");

            Assert.Equal(3, StatsJson.Int(stats, "activePeople", "people"));
            Assert.Equal(6, StatsJson.Int(stats, "activePeople", "personDays"));
            Assert.Equal([1, 3], StatsJson.Ints(stats, "activePeople", "peopleCounts"));
            Assert.Equal([1, 5], StatsJson.Ints(stats, "activePeople", "personDayCounts"));
        }

        [Fact]
        public async Task Stats_ActivePeople_GuestsAreOnlyTheGuestsAmongThePeopleActiveInTheRange()
        {
            Guid ana = await _app.AddPersonAsync();
            Guid guestInRange = await _app.AddGuestAsync();
            Guid guestOutOfRange = await _app.AddGuestAsync();
            await _app.AddEventsAsync(
                UsageEvent.Active(ana, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.Active(guestInRange, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.Active(guestInRange, Moment.At("2026-11-03T10:00:00Z")),
                UsageEvent.Active(guestOutOfRange, Moment.At("2026-10-20T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(2, StatsJson.Int(stats, "activePeople", "people"));
            Assert.Equal(1, StatsJson.Int(stats, "activePeople", "guests"));
        }

        [Fact]
        public async Task Stats_ActivePeople_AGuestWhoLaterMadeAnAccountIsNotCountedAsAGuest()
        {
            Guid upgraded = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(upgraded, SignUpMethod.Guest, Moment.At("2026-11-01T09:00:00Z")),
                UsageEvent.Active(upgraded, Moment.At("2026-11-01T10:00:00Z")),
                UsageEvent.GuestUpgraded(upgraded, SignUpMethod.Email, Moment.At("2026-11-02T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(1, StatsJson.Int(stats, "activePeople", "people"));
            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "guests"));
        }

        [Fact]
        public async Task Stats_ActivePeople_PreviousPeriodHasItsOwnDistinctPeopleAndPersonDays()
        {
            Guid ana = await _app.AddPersonAsync();
            Guid bojan = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.Active(ana, Moment.At("2026-10-27T10:00:00Z")),
                UsageEvent.Active(bojan, Moment.At("2026-10-28T10:00:00Z")),
                UsageEvent.Active(ana, Moment.At("2026-10-29T10:00:00Z")),
                UsageEvent.Active(ana, Moment.At("2026-10-30T10:00:00Z")),
                UsageEvent.Active(ana, Moment.At("2026-11-01T10:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(1, StatsJson.Int(stats, "activePeople", "people"));
            Assert.Equal(2, StatsJson.Int(stats, "activePeople", "previousPeople"));
            Assert.Equal(1, StatsJson.Int(stats, "activePeople", "personDays"));
            Assert.Equal(3, StatsJson.Int(stats, "activePeople", "previousPersonDays"));
        }

        [Fact]
        public async Task Stats_ActivePeople_OtherEventTypesDoNotMakeAPersonActive()
        {
            Guid ana = await _app.AddPersonAsync();
            await _app.AddEventsAsync(
                UsageEvent.SignedUp(ana, SignUpMethod.Email, Moment.At("2026-11-02T10:00:00Z")),
                UsageEvent.ExpenseAdded(ana, Moment.At("2026-11-02T11:00:00Z")),
                UsageEvent.GroupCreated(ana, Moment.At("2026-11-02T12:00:00Z")));

            JsonElement stats = await StatsAsync(from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "people"));
            Assert.Equal(0, StatsJson.Int(stats, "activePeople", "personDays"));
        }
    }
}
