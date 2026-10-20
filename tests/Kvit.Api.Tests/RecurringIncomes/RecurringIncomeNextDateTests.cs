using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class RecurringIncomeNextDateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(15, "2026-10-01", "2026-10-05T12:00:00Z", "2026-10-15")]
        [InlineData(15, "2026-10-01", "2026-10-14T12:00:00Z", "2026-10-15")]
        [InlineData(15, "2026-10-01", "2026-10-15T12:00:00Z", "2026-11-15")]
        [InlineData(15, "2026-10-01", "2026-10-16T12:00:00Z", "2026-11-15")]
        [InlineData(15, "2026-10-01", "2026-12-20T12:00:00Z", "2027-01-15")]
        [InlineData(1, "2026-10-01", "2026-10-01T12:00:00Z", "2026-11-01")]
        [InlineData(31, "2026-01-01", "2026-04-10T12:00:00Z", "2026-04-30")]
        [InlineData(31, "2026-01-01", "2026-01-31T12:00:00Z", "2026-02-28")]
        [InlineData(31, "2026-01-01", "2026-02-27T12:00:00Z", "2026-02-28")]
        [InlineData(31, "2026-01-01", "2026-02-28T12:00:00Z", "2026-03-31")]
        [InlineData(30, "2028-01-01", "2028-02-10T12:00:00Z", "2028-02-29")]
        [InlineData(29, "2027-01-01", "2027-02-10T12:00:00Z", "2027-02-28")]
        public async Task List_MonthlyIncome_ShowsTheFirstDueDateStrictlyAfterTodayWithMonthEndsClamped(int dayOfMonth, string startsOn, string utcNow, string expectedNextDate)
        {
            CatchUpSteps.SetNow(_app, utcNow);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: dayOfMonth, startsOn: startsOn));

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal(JsonValueKind.String, row.GetProperty("nextDate").ValueKind);
            Assert.Equal(expectedNextDate, row.GetProperty("nextDate").GetString());
        }

        [Theory]
        [InlineData(3, "2026-10-01", "2026-10-05T12:00:00Z", "2026-10-07")]
        [InlineData(3, "2026-10-01", "2026-10-07T12:00:00Z", "2026-10-14")]
        [InlineData(3, "2026-10-01", "2026-10-08T12:00:00Z", "2026-10-14")]
        [InlineData(0, "2026-10-01", "2026-10-05T12:00:00Z", "2026-10-11")]
        [InlineData(0, "2026-10-01", "2026-10-11T12:00:00Z", "2026-10-18")]
        [InlineData(6, "2026-10-01", "2026-10-30T12:00:00Z", "2026-10-31")]
        [InlineData(6, "2026-10-01", "2026-10-31T12:00:00Z", "2026-11-07")]
        [InlineData(4, "2026-10-01", "2026-12-31T12:00:00Z", "2027-01-07")]
        public async Task List_WeeklyIncome_ShowsTheFirstChosenWeekdayStrictlyAfterToday(int dayOfWeek, string startsOn, string utcNow, string expectedNextDate)
        {
            CatchUpSteps.SetNow(_app, utcNow);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Weekly(dayOfWeek: dayOfWeek, startsOn: startsOn));

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal(expectedNextDate, row.GetProperty("nextDate").GetString());
        }

        [Theory]
        [InlineData("Monthly", 15, null, "2026-12-15", "2026-10-05T12:00:00Z", "2026-12-15")]
        [InlineData("Monthly", 6, null, "2026-10-06", "2026-10-05T12:00:00Z", "2026-10-06")]
        [InlineData("Monthly", 5, null, "2026-10-05", "2026-10-05T12:00:00Z", "2026-11-05")]
        [InlineData("Weekly", null, 3, "2026-10-07", "2026-10-05T12:00:00Z", "2026-10-07")]
        [InlineData("Monthly", 15, null, "2026-12-01", "2026-10-05T12:00:00Z", "2026-12-15")]
        [InlineData("Weekly", null, 3, "2026-10-09", "2026-10-05T12:00:00Z", "2026-10-14")]
        [InlineData("Monthly", 15, null, "2027-10-05", "2026-10-05T12:00:00Z", "2027-10-15")]
        public async Task List_IncomeStartingInTheFuture_ShowsTheFirstDueDateOnOrAfterTheStartDate(string frequency, int? dayOfMonth, int? dayOfWeek, string startsOn, string utcNow, string expectedNextDate)
        {
            CatchUpSteps.SetNow(_app, utcNow);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Of(frequency, dayOfMonth, dayOfWeek, startsOn: startsOn));

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal(expectedNextDate, row.GetProperty("nextDate").GetString());
        }

        [Theory]
        [InlineData(15, "2026-10-01", "2026-10-15", "2026-10-05T12:00:00Z", "2026-10-15")]
        [InlineData(15, "2026-10-01", "2026-10-14", "2026-10-05T12:00:00Z", null)]
        [InlineData(15, "2026-10-01", "2026-12-31", "2026-10-16T12:00:00Z", "2026-11-15")]
        [InlineData(15, "2026-10-01", "2026-11-15", "2026-10-16T12:00:00Z", "2026-11-15")]
        [InlineData(15, "2026-10-01", "2026-11-14", "2026-10-16T12:00:00Z", null)]
        [InlineData(15, "2026-10-01", "2026-10-15", "2026-10-15T12:00:00Z", null)]
        [InlineData(15, "2026-08-01", "2026-09-20", "2026-10-20T12:00:00Z", null)]
        [InlineData(15, "2026-10-01", "2026-10-01", "2026-10-20T12:00:00Z", null)]
        public async Task List_MonthlyIncomeWithAnEndDate_ShowsNullOnceNoDueDateIsLeftBeforeItEnds(int dayOfMonth, string startsOn, string endsOn, string utcNow, string? expectedNextDate)
        {
            CatchUpSteps.SetNow(_app, utcNow);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: dayOfMonth, startsOn: startsOn, endsOn: endsOn));

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            JsonElement nextDate = row.GetProperty("nextDate");
            if (expectedNextDate is null)
            {
                Assert.Equal(JsonValueKind.Null, nextDate.ValueKind);
            }
            else
            {
                Assert.Equal(expectedNextDate, nextDate.GetString());
            }
        }

        [Theory]
        [InlineData("2026-10-10", "2026-10-08T12:00:00Z")]
        [InlineData("2026-10-13", "2026-10-08T12:00:00Z")]
        public async Task List_WeeklyIncomeWhoseNextWeekdayIsPastItsEndDate_ShowsNull(string endsOn, string utcNow)
        {
            CatchUpSteps.SetNow(_app, utcNow);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Weekly(dayOfWeek: 3, startsOn: "2026-10-01", endsOn: endsOn));

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal(JsonValueKind.Null, row.GetProperty("nextDate").ValueKind);
        }

        [Fact]
        public async Task List_WeeklyIncomeWhoseNextWeekdayIsTheEndDate_ShowsThatDate()
        {
            CatchUpSteps.SetNow(_app, "2026-10-08T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Weekly(dayOfWeek: 3, startsOn: "2026-10-01", endsOn: "2026-10-14"));

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal("2026-10-14", row.GetProperty("nextDate").GetString());
        }

        [Theory]
        [InlineData("Pacific/Kiritimati", "2026-10-31T10:30:00Z", "2026-12-01")]
        [InlineData("Pacific/Honolulu", "2026-10-31T10:30:00Z", "2026-11-01")]
        [InlineData("Europe/Skopje", "2026-10-31T22:59:00Z", "2026-11-01")]
        [InlineData("Europe/Skopje", "2026-10-31T23:00:00Z", "2026-12-01")]
        [InlineData("UTC", "2026-10-31T23:59:00Z", "2026-11-01")]
        [InlineData("UTC", "2026-11-01T00:00:00Z", "2026-12-01")]
        public async Task List_MonthlyDay1Income_CountsTodayInTheUsersTimeZone(string timeZone, string utcNow, string expectedNextDate)
        {
            CatchUpSteps.SetNow(_app, utcNow);
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, timeZone);
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 1, startsOn: "2026-10-01"));

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal(expectedNextDate, row.GetProperty("nextDate").GetString());
        }

        [Fact]
        public async Task List_SeveralIncomes_EachRowHasItsOwnNextDate()
        {
            CatchUpSteps.SetNow(_app, "2026-10-05T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid monthly = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 20));
            Guid weekly = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Weekly(dayOfWeek: 5));
            Guid ended = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 2, endsOn: "2026-10-04"));

            List<JsonElement> rows = await RecurringIncomeSteps.ListRowsAsync(user.Client);

            Dictionary<Guid, JsonElement> nextDates = rows.ToDictionary(row => row.GetProperty("id").GetGuid(), row => row.GetProperty("nextDate"));
            Assert.Equal("2026-10-20", nextDates[monthly].GetString());
            Assert.Equal("2026-10-09", nextDates[weekly].GetString());
            Assert.Equal(JsonValueKind.Null, nextDates[ended].ValueKind);
        }

        [Fact]
        public async Task List_AfterCatchUpCreatedTheEntries_NextDateIsTheSameAsBefore()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15, startsOn: "2026-08-01"));
            JsonElement before = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));
            await CatchUpSteps.CatchUpAsync(user.Client);

            JsonElement after = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal("2026-11-15", before.GetProperty("nextDate").GetString());
            Assert.Equal(before.GetRawText(), after.GetRawText());
        }

        [Fact]
        public async Task List_AfterTheOnlyEntryWasDeleted_NextDateIsStillTheNextDueDateNotTheDeletedOne()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 15), deletedAt: _app.Clock.GetUtcNow());

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal("2026-11-15", row.GetProperty("nextDate").GetString());
        }

        [Fact]
        public async Task List_IncomeStartedMoreThanTwoYearsAgo_ShowsTheNextDueDateAfterToday()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2021, 10, 1));

            JsonElement row = Assert.Single(await RecurringIncomeSteps.ListRowsAsync(user.Client));

            Assert.Equal("2026-11-15", row.GetProperty("nextDate").GetString());
        }

        [Fact]
        public async Task Create_MonthlyIncome_AnswersTheNextDate()
        {
            CatchUpSteps.SetNow(_app, "2026-10-05T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("2026-10-15", body.GetProperty("nextDate").GetString());
        }

        [Fact]
        public async Task Create_WeeklyIncome_AnswersTheNextDate()
        {
            CatchUpSteps.SetNow(_app, "2026-10-05T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Weekly(dayOfWeek: 3));

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("2026-10-07", body.GetProperty("nextDate").GetString());
        }

        [Fact]
        public async Task Create_IncomeThatAlreadyEnded_AnswersANullNextDate()
        {
            CatchUpSteps.SetNow(_app, "2026-10-05T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15, startsOn: "2026-09-01", endsOn: "2026-09-30"));

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("nextDate").ValueKind);
        }

        [Fact]
        public async Task Create_SameClientRequestIdTwice_AnswersTheSameNextDateBothTimes()
        {
            CatchUpSteps.SetNow(_app, "2026-10-05T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid clientRequestId = Guid.CreateVersion7();
            RecurringIncomeBody body = RecurringIncomeBody.Monthly(dayOfMonth: 15);

            JsonElement first = await AuthRequests.ReadJsonAsync(await RecurringIncomeRequests.CreateAsync(user.Client, body, clientRequestId));
            JsonElement second = await AuthRequests.ReadJsonAsync(await RecurringIncomeRequests.CreateAsync(user.Client, body, clientRequestId));

            Assert.Equal("2026-10-15", first.GetProperty("nextDate").GetString());
            Assert.Equal(first.GetRawText(), second.GetRawText());
        }

        [Fact]
        public async Task Edit_DayOfMonth_AnswersTheNextDateOfTheNewRhythm()
        {
            CatchUpSteps.SetNow(_app, "2026-10-05T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15));

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(dayOfMonth: 20));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("2026-10-20", body.GetProperty("nextDate").GetString());
        }

        [Fact]
        public async Task Edit_AfterTheFirstEntryWasMade_AnswersTheFirstNewDueDateAfterToday()
        {
            CatchUpSteps.SetNow(_app, "2026-10-01T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 1, startsOn: "2026-10-01"));
            await CatchUpSteps.CatchUpAsync(user.Client);
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(dayOfMonth: 5, startsOn: "2026-10-01"));

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("2026-11-05", body.GetProperty("nextDate").GetString());
        }

        [Fact]
        public async Task Edit_EndDateBeforeTheNextDueDate_AnswersANullNextDate()
        {
            CatchUpSteps.SetNow(_app, "2026-10-05T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15));

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(dayOfMonth: 15, endsOn: "2026-10-10"));

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("nextDate").ValueKind);
        }

        [Fact]
        public async Task Edit_MonthlyToWeekly_AnswersTheNextWeekday()
        {
            CatchUpSteps.SetNow(_app, "2026-10-05T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15));

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Weekly(dayOfWeek: 4));

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("2026-10-08", body.GetProperty("nextDate").GetString());
        }

        [Fact]
        public async Task Edit_UserInAnEarlyTimeZone_AnswersTheNextDateAfterTheirLocalToday()
        {
            CatchUpSteps.SetNow(_app, "2026-10-31T10:30:00Z");
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, "Pacific/Kiritimati");
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 1, startsOn: "2026-10-01"));

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(dayOfMonth: 2, startsOn: "2026-10-01"));

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("2026-11-02", body.GetProperty("nextDate").GetString());
        }
    }
}
