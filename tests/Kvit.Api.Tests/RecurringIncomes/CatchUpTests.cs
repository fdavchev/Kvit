using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CatchUpTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task CatchUp_UserWithoutRepeatingIncomes_Answers200WithCreatedZero()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task CatchUp_MonthlyIncomeStartedInAugust_CreatesOneEntryForEveryDueDateUpToToday()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(3, created);
            Assert.Equal(["2026-08-15", "2026-09-15", "2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_NewEntry_CopiesTheIncomeAndLinksItToTheOwner()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, amountMinor: 4_500_000, currency: "MKD", title: "Salary", dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal("Income", await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "kind"));
            Assert.Equal("4500000", await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "amount_minor"));
            Assert.Equal("MKD", await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "currency"));
            Assert.Equal("Salary", await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "note"));
            Assert.Equal("2026-10-15", await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "entry_date"));
            Assert.Null(await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "category_id"));
            Assert.Equal(recurringIncomeId.ToString(), await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "recurring_income_id"));
            Assert.Null(await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "client_request_id"));
            Assert.Equal(user.UserId.ToString(), await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "user_id"));
            Assert.Equal("2026-10-20 12:00:00", await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "created_at AT TIME ZONE 'UTC'"));
            Assert.Null(await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "updated_at"));
            Assert.Null(await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "deleted_at"));
        }

        [Fact]
        public async Task CatchUp_IncomeInEuro_CreatesEntriesInEuroWithTheSameCents()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, amountMinor: 123_456, currency: "EUR", dayOfMonth: 10, startsOn: new DateOnly(2026, 10, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal("EUR", await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "currency"));
            Assert.Equal("123456", await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "amount_minor"));
        }

        [Fact]
        public async Task CatchUp_IncomeWithoutATitle_CreatesEntriesWithANullNote()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, title: null, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Null(await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "note"));
        }

        [Fact]
        public async Task CatchUp_WeeklyIncomeOnWednesdays_CreatesAnEntryForEveryWednesdayUpToToday()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 3, startsOn: new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(2, created);
            Assert.Equal(["2026-10-07", "2026-10-14"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_WeeklyIncomeOnSundays_UsesZeroAsSunday()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 0, startsOn: new DateOnly(2026, 10, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(["2026-10-04", "2026-10-11", "2026-10-18"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_SeveralIncomes_CreatesEntriesForEachAndCountsThemAll()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid salary = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1));
            Guid rent = await RecurringIncomeRows.InsertAsync(_app, user.UserId, currency: "EUR", amountMinor: 20_000, dayOfMonth: 1, startsOn: new DateOnly(2026, 9, 1));
            Guid tips = await RecurringIncomeRows.InsertAsync(_app, user.UserId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 5, startsOn: new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(3 + 2 + 3, created);
            Assert.Equal(["2026-08-15", "2026-09-15", "2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, salary));
            Assert.Equal(["2026-09-01", "2026-10-01"], await CatchUpSteps.EntryDatesAsync(_app, rent));
            Assert.Equal(["2026-10-02", "2026-10-09", "2026-10-16"], await CatchUpSteps.EntryDatesAsync(_app, tips));
            Assert.Equal(8, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task CatchUp_DueDateIsTodayInTheUsersTimeZone_CreatesTheEntryOfToday()
        {
            CatchUpSteps.SetNow(_app, "2026-10-15T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(1, created);
            Assert.Equal(["2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_DueDateIsTomorrow_CreatesNothingYet()
        {
            CatchUpSteps.SetNow(_app, "2026-10-14T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Empty(await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_StartDateOnADueDate_CreatesThatDay()
        {
            CatchUpSteps.SetNow(_app, "2026-10-15T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 15));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(["2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_StartDateOneDayAfterADueDate_SkipsThatMonthsDueDate()
        {
            CatchUpSteps.SetNow(_app, "2026-11-20T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 16));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(1, created);
            Assert.Equal(["2026-11-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_Day31StartedInJanuary_FallsOnTheLastDayOfFebruary()
        {
            CatchUpSteps.SetNow(_app, "2026-03-01T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 31, startsOn: new DateOnly(2026, 1, 31));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(2, created);
            Assert.Equal(["2026-01-31", "2026-02-28"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_Day30InALeapYear_FallsOnTheLeapDay()
        {
            CatchUpSteps.SetNow(_app, "2028-03-05T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 30, startsOn: new DateOnly(2028, 1, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(["2028-01-30", "2028-02-29"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_Day31StartedInFebruary_FallsOnTheLastDayOfEveryShortMonth()
        {
            CatchUpSteps.SetNow(_app, "2027-01-15T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 31, startsOn: new DateOnly(2026, 2, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(11, created);
            Assert.Equal(
                ["2026-02-28", "2026-03-31", "2026-04-30", "2026-05-31", "2026-06-30", "2026-07-31", "2026-08-31", "2026-09-30", "2026-10-31", "2026-11-30", "2026-12-31"],
                await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_IncomeStartingInTheFuture_CreatesNothingUntilItsDay()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 11, 15));

            int beforeItsDay = await CatchUpSteps.CatchUpAsync(user.Client);
            CatchUpSteps.SetNow(_app, "2026-11-14T12:00:00Z");
            int theDayBefore = await CatchUpSteps.CatchUpAsync(user.Client);
            CatchUpSteps.SetNow(_app, "2026-11-15T12:00:00Z");
            int onItsDay = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, beforeItsDay);
            Assert.Equal(0, theDayBefore);
            Assert.Equal(1, onItsDay);
            Assert.Equal(["2026-11-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Theory]
        [InlineData("2026-09-20", new[] { "2026-08-15", "2026-09-15" })]
        [InlineData("2026-09-15", new[] { "2026-08-15", "2026-09-15" })]
        [InlineData("2026-09-14", new[] { "2026-08-15" })]
        [InlineData("2026-08-15", new[] { "2026-08-15" })]
        [InlineData("2026-08-14", new string[0])]
        public async Task CatchUp_IncomeThatEndedInThePast_StopsAtItsEndDateIncludingTheEndDate(string endsOn, string[] expectedDates)
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1), endsOn: CatchUpSteps.DateOf(endsOn));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(expectedDates.Length, created);
            Assert.Equal(expectedDates, await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_IncomeThatEndsInTheFuture_StopsAtTodayNotAtTheEndDate()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 9, 1), endsOn: new DateOnly(2027, 6, 30));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(["2026-09-15", "2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_IncomeThatEndedLongAgoAndWasCaughtUp_NeverCreatesAnythingAfterItsEndDate()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1), endsOn: new DateOnly(2026, 9, 20));
            await CatchUpSteps.CatchUpAsync(user.Client);
            CatchUpSteps.SetNow(_app, "2026-12-20T12:00:00Z");

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Equal(["2026-08-15", "2026-09-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_StoppedIncome_CreatesNothing()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1), stoppedAt: _app.Clock.GetUtcNow().AddDays(-1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Empty(await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_StoppedThroughTheApi_CreatesNothingMoreAndKeepsTheEntriesAlreadyMade()
        {
            CatchUpSteps.SetNow(_app, "2026-10-01T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 1, startsOn: "2026-10-01"));
            await CatchUpSteps.CatchUpAsync(user.Client);
            await RecurringIncomeRequests.StopAsync(user.Client, recurringIncomeId);
            CatchUpSteps.SetNow(_app, "2026-11-20T12:00:00Z");

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Equal(["2026-10-01"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_StoppedIncomeNextToAnActiveOne_OnlyTheActiveOneCreatesEntries()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid stopped = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1), stoppedAt: _app.Clock.GetUtcNow());
            Guid active = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(1, created);
            Assert.Empty(await CatchUpSteps.EntryDatesAsync(_app, stopped));
            Assert.Equal(["2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, active));
        }

        [Fact]
        public async Task CatchUp_SecondCallRightAfterTheFirst_Answers200WithCreatedZeroAndChangesNothing()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1));
            await CatchUpSteps.CatchUpAsync(user.Client);
            string entriesBefore = await PersonalEntrySteps.SnapshotAsync(_app);

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Equal(entriesBefore, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task CatchUp_LaterTheSameDay_CreatesNothingNew()
        {
            CatchUpSteps.SetNow(_app, "2026-10-15T08:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            await CatchUpSteps.CatchUpAsync(user.Client);
            CatchUpSteps.SetNow(_app, "2026-10-15T15:00:00Z");

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
        }

        [Fact]
        public async Task CatchUp_NextMonthAfterACatchUp_CreatesOnlyTheNewEntry()
        {
            CatchUpSteps.SetNow(_app, "2026-10-15T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            await CatchUpSteps.CatchUpAsync(user.Client);
            CatchUpSteps.SetNow(_app, "2026-11-15T12:00:00Z");

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(1, created);
            Assert.Equal(["2026-10-15", "2026-11-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_NewIncomeSavedThroughTheApi_CreatesNothingUntilCatchUpRuns()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);

            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15, startsOn: "2026-10-01"));

            Assert.Empty(await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
            Assert.Equal(1, await CatchUpSteps.CatchUpAsync(user.Client));
            Assert.Equal(["2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_Entries_WriteNoActivityOrUsageEventsAndLeaveTheIncomeRowsUntouched()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1));
            int eventsBefore = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);
            string incomesBefore = await RecurringIncomeRows.SnapshotAsync(_app);

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(eventsBefore, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
            Assert.Equal(incomesBefore, await RecurringIncomeRows.SnapshotAsync(_app));
        }
    }
}
