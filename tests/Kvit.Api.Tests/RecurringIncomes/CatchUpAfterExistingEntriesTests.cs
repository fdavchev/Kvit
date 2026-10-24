using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CatchUpAfterExistingEntriesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task CatchUp_LastEntryOnTheLastDueDate_CreatesOnlyTheDatesAfterItAndNeverBackFillsEarlierOnes()
        {
            CatchUpSteps.SetNow(_app, "2026-11-20T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 8, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(1, created);
            Assert.Equal(["2026-10-01", "2026-11-01"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_DeletedLastEntry_IsNotCreatedAgain()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 10, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 1), deletedAt: _app.Clock.GetUtcNow().AddDays(-1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Equal(["2026-10-01"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
            Assert.NotNull(await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "deleted_at"));
        }

        [Fact]
        public async Task CatchUp_DeletedLastEntryAfterALiveOne_CreatesOnlyTheNextDateAfterIt()
        {
            CatchUpSteps.SetNow(_app, "2026-11-20T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 9, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 9, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 1), deletedAt: _app.Clock.GetUtcNow().AddDays(-1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(1, created);
            Assert.Equal(["2026-09-01", "2026-10-01", "2026-11-01"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_DeletedEntryBeforeALiveOne_IsNotCreatedAgain()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 9, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 9, 1), deletedAt: _app.Clock.GetUtcNow().AddDays(-1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Equal(["2026-09-01", "2026-10-01"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_EntryDeletedThroughTheApi_IsNotCreatedAgainOnTheNextCatchUp()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            await CatchUpSteps.CatchUpAsync(user.Client);
            List<Guid> entryIds = await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client);
            HttpResponseMessage deleted = await PersonalEntryRequests.DeleteAsync(user.Client, Assert.Single(entryIds));
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Equal(["2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
            Assert.Empty(await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));
        }

        [Fact]
        public async Task CatchUp_LastEntryOlderThanTheTwentyFourMonthWindow_StillStartsAtTheOldestAllowedMonth()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2022, 1, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2023, 1, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            List<string> dates = await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId);
            Assert.Equal(24, created);
            Assert.Equal(25, dates.Count);
            Assert.Equal("2023-01-01", dates[0]);
            Assert.Equal("2024-11-01", dates[1]);
            Assert.Equal("2026-10-01", dates[^1]);
        }

        [Fact]
        public async Task CatchUp_EntriesOfAnotherIncomeOfTheSameUser_DoNotCountAsTheLastEntry()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid salary = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 9, 1));
            Guid rent = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 9, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, salary, new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(2, created);
            Assert.Equal(["2026-10-01"], await CatchUpSteps.EntryDatesAsync(_app, salary));
            Assert.Equal(["2026-09-01", "2026-10-01"], await CatchUpSteps.EntryDatesAsync(_app, rent));
        }

        [Fact]
        public async Task CatchUp_IncomeEnteredOnceByHandOnTheSameDay_DoesNotBlockTheRepeatingEntry()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(entryDate: "2026-10-15"));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(1, created);
            Assert.Equal(["2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
            Assert.Equal(2, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task CatchUp_DayOfMonthEditedAfterTheFirstEntry_NeverBackFillsAndTheNextEntryIsOnTheNewDay()
        {
            CatchUpSteps.SetNow(_app, "2026-10-01T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 1, startsOn: "2026-10-01"));
            Assert.Equal(1, await CatchUpSteps.CatchUpAsync(user.Client));
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            HttpResponseMessage edited = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(dayOfMonth: 5, startsOn: "2026-10-01"));
            Assert.Equal(HttpStatusCode.OK, edited.StatusCode);

            int onOctober20 = await CatchUpSteps.CatchUpAsync(user.Client);
            CatchUpSteps.SetNow(_app, "2026-11-04T12:00:00Z");
            int onNovember4 = await CatchUpSteps.CatchUpAsync(user.Client);
            CatchUpSteps.SetNow(_app, "2026-11-05T12:00:00Z");
            int onNovember5 = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, onOctober20);
            Assert.Equal(0, onNovember4);
            Assert.Equal(1, onNovember5);
            Assert.Equal(["2026-10-01", "2026-11-05"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_AmountEditedAfterTheFirstEntry_KeepsTheOldEntryAndUsesTheNewAmountFromNowOn()
        {
            CatchUpSteps.SetNow(_app, "2026-10-01T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(4_500_000, dayOfMonth: 1, startsOn: "2026-10-01"));
            await CatchUpSteps.CatchUpAsync(user.Client);
            HttpResponseMessage edited = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(5_000_000, title: "Raise", dayOfMonth: 1, startsOn: "2026-10-01"));
            Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
            CatchUpSteps.SetNow(_app, "2026-11-01T12:00:00Z");

            await CatchUpSteps.CatchUpAsync(user.Client);

            List<string?> amounts = await _app.QueryAsync(
                "SELECT amount_minor::text || ' ' || note FROM personal_entries WHERE recurring_income_id = @id ORDER BY entry_date",
                new NpgsqlParameter("id", recurringIncomeId));
            Assert.Equal(["4500000 Salary", "5000000 Raise"], amounts);
        }
    }
}
