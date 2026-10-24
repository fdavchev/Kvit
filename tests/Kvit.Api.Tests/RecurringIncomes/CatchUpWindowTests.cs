using System.Globalization;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CatchUpWindowTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly DateOnly FiveYearsAgo = new(2021, 10, 1);

        [Fact]
        public async Task CatchUp_MonthlyDay1IncomeStartedFiveYearsAgo_CreatesExactlyTheLast24Months()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: FiveYearsAgo);

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            List<string> dates = await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId);
            Assert.Equal(24, created);
            Assert.Equal(24, dates.Count);
            Assert.Equal("2024-11-01", dates[0]);
            Assert.Equal("2026-10-01", dates[^1]);
            Assert.Equal(EveryFirstOfTheMonthFromNovember2024(), dates);
        }

        [Fact]
        public async Task CatchUp_MonthlyDay31IncomeStartedFiveYearsAgo_StartsOnTheClampedDayOfTheOldestAllowedMonth()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 31, startsOn: FiveYearsAgo);

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            List<string> dates = await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId);
            Assert.Equal(23, created);
            Assert.Equal("2024-11-30", dates[0]);
            Assert.Equal("2026-09-30", dates[^1]);
        }

        [Fact]
        public async Task CatchUp_WeeklyFridayIncomeStartedFiveYearsAgo_NeverGoesBeforeTheOldestAllowedMonth()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 5, startsOn: FiveYearsAgo);

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            List<string> dates = await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId);
            Assert.Equal(103, created);
            Assert.Equal(103, dates.Count);
            Assert.Equal("2024-11-01", dates[0]);
            Assert.Equal("2026-10-16", dates[^1]);
        }

        [Fact]
        public async Task CatchUp_StartDateInsideTheWindow_IsNotMovedByTheWindow()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2025, 3, 10));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            List<string> dates = await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId);
            Assert.Equal(19, created);
            Assert.Equal("2025-04-01", dates[0]);
            Assert.Equal("2026-10-01", dates[^1]);
        }

        [Fact]
        public async Task CatchUp_StartDateOnTheFirstDayOfTheOldestAllowedMonth_IncludesThatDay()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2024, 11, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(24, created);
            Assert.Equal("2024-11-01", (await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId))[0]);
        }

        [Fact]
        public async Task CatchUp_DueDateOneDayBeforeTheOldestAllowedMonth_IsLeftOut()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 31, startsOn: new DateOnly(2024, 10, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.DoesNotContain("2024-10-31", await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_UserWhoseLocalMonthIsAlreadyNovember_CountsTheWindowFromNovember()
        {
            CatchUpSteps.SetNow(_app, "2026-10-31T23:30:00Z");
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, CatchUpSteps.Skopje);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: FiveYearsAgo);

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            List<string> dates = await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId);
            Assert.Equal(24, created);
            Assert.Equal("2024-12-01", dates[0]);
            Assert.Equal("2026-11-01", dates[^1]);
        }

        [Fact]
        public async Task CatchUp_UserWhoseLocalMonthIsStillOctoberWhileUtcIsNovember_CountsTheWindowFromOctober()
        {
            CatchUpSteps.SetNow(_app, "2026-11-01T05:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, "Pacific/Honolulu");
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: FiveYearsAgo);

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            List<string> dates = await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId);
            Assert.Equal(24, created);
            Assert.Equal("2024-11-01", dates[0]);
            Assert.Equal("2026-10-01", dates[^1]);
        }

        [Fact]
        public async Task CatchUp_SecondCallAfterTheClampedFirstCall_CreatesNothing()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: FiveYearsAgo);
            await CatchUpSteps.CatchUpAsync(user.Client);

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Equal(24, (await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId)).Count);
        }

        private static List<string> EveryFirstOfTheMonthFromNovember2024()
        {
            return [.. Enumerable.Range(0, 24).Select(month => new DateOnly(2024, 11, 1).AddMonths(month).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))];
        }
    }
}
