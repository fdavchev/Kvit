using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CatchUpTimeZoneTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData("Pacific/Kiritimati", "2026-10-31T10:30:00Z", new[] { "2026-11-01" })]
        [InlineData("Pacific/Kiritimati", "2026-10-31T09:59:00Z", new string[0])]
        [InlineData("Pacific/Honolulu", "2026-10-31T10:30:00Z", new string[0])]
        [InlineData("Pacific/Honolulu", "2026-11-01T09:59:00Z", new string[0])]
        [InlineData("Pacific/Honolulu", "2026-11-01T10:00:00Z", new[] { "2026-11-01" })]
        [InlineData("UTC", "2026-10-31T23:59:00Z", new string[0])]
        [InlineData("UTC", "2026-11-01T00:00:00Z", new[] { "2026-11-01" })]
        [InlineData("Europe/Skopje", "2026-10-31T22:59:00Z", new string[0])]
        [InlineData("Europe/Skopje", "2026-10-31T23:00:00Z", new[] { "2026-11-01" })]
        [InlineData("America/New_York", "2026-11-01T03:59:00Z", new string[0])]
        [InlineData("America/New_York", "2026-11-01T04:00:00Z", new[] { "2026-11-01" })]
        public async Task CatchUp_MonthlyDay1IncomeAroundLocalMidnightOfTheFirst_FollowsTheDateInTheUsersTimeZone(string timeZone, string utcInstant, string[] expectedNewDates)
        {
            CatchUpSteps.SetNow(_app, utcInstant);
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, timeZone);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 10, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(expectedNewDates.Length, created);
            Assert.Equal(["2026-10-01", .. expectedNewDates], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Theory]
        [InlineData("Pacific/Kiritimati", "2026-10-30T10:30:00Z", new[] { "2026-10-31" })]
        [InlineData("UTC", "2026-10-30T10:30:00Z", new string[0])]
        [InlineData("Europe/Skopje", "2026-10-31T22:59:00Z", new[] { "2026-10-31" })]
        [InlineData("Europe/Skopje", "2026-10-30T22:59:00Z", new string[0])]
        [InlineData("Pacific/Honolulu", "2026-10-31T09:59:00Z", new string[0])]
        [InlineData("Pacific/Honolulu", "2026-10-31T10:00:00Z", new[] { "2026-10-31" })]
        public async Task CatchUp_MonthlyDay31IncomeAroundLocalMidnightOfTheMonthEnd_FollowsTheDateInTheUsersTimeZone(string timeZone, string utcInstant, string[] expectedNewDates)
        {
            CatchUpSteps.SetNow(_app, utcInstant);
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, timeZone);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 31, startsOn: new DateOnly(2026, 9, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 9, 30));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(expectedNewDates.Length, created);
            Assert.Equal(["2026-09-30", .. expectedNewDates], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_WeeklyIncomeOnTheDayClocksGoForwardInSkopje_CreatesThatDaysEntryOnce()
        {
            CatchUpSteps.SetNow(_app, "2026-03-29T00:30:00Z");
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, CatchUpSteps.Skopje);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 0, startsOn: new DateOnly(2026, 3, 22));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(2, created);
            Assert.Equal(["2026-03-22", "2026-03-29"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_TwoUsersInDifferentTimeZonesAtTheSameInstant_EachGetsTheirOwnLocalToday()
        {
            CatchUpSteps.SetNow(_app, "2026-10-31T10:30:00Z");
            SignedInUser early = await CatchUpSteps.SignUpInAsync(_app, "Pacific/Kiritimati");
            SignedInUser late = await CatchUpSteps.SignUpInAsync(_app, "Pacific/Honolulu", "Bojan");
            Guid earlyIncome = await RecurringIncomeRows.InsertAsync(_app, early.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 10, 1));
            Guid lateIncome = await RecurringIncomeRows.InsertAsync(_app, late.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 10, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, early.UserId, earlyIncome, new DateOnly(2026, 10, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, late.UserId, lateIncome, new DateOnly(2026, 10, 1));

            int createdForEarly = await CatchUpSteps.CatchUpAsync(early.Client);
            int createdForLate = await CatchUpSteps.CatchUpAsync(late.Client);

            Assert.Equal(1, createdForEarly);
            Assert.Equal(0, createdForLate);
        }
    }
}
