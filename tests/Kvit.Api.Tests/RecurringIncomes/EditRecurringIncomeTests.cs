using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class EditRecurringIncomeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Edit_OwnIncome_Answers200WithTheUpdatedRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(5_000_000, "MKD", " Raise ", 20, "2026-10-01", "2028-01-31"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(RecurringIncomeRequests.RowFieldNames, body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.Equal(recurringIncomeId, body.GetProperty("id").GetGuid());
            Assert.Equal(5_000_000, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("Raise", body.GetProperty("title").GetString());
            Assert.Equal(20, body.GetProperty("dayOfMonth").GetInt32());
            Assert.Equal("2028-01-31", body.GetProperty("endsOn").GetString());
            Assert.Equal(ExpensesApp.ClockStart, body.GetProperty("createdAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Edit_OwnIncome_StoresTheNewValuesKeepsTheIdentityAndWritesNoEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            string clientRequestIdBefore = (await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "client_request_id"))!;
            string createdAtBefore = (await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "created_at"))!;
            int eventsBefore = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(3_000_000, "EUR", "Rent", 1));

            Assert.Equal("3000000", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "amount_minor"));
            Assert.Equal("EUR", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "currency"));
            Assert.Equal("Rent", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "title"));
            Assert.Equal("1", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "day_of_month"));
            Assert.Equal(user.UserId.ToString(), await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "user_id"));
            Assert.Equal(clientRequestIdBefore, await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "client_request_id"));
            Assert.Equal(createdAtBefore, await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "created_at"));
            Assert.Null(await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "stopped_at"));
            Assert.Equal(eventsBefore, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Edit_MonthlyToWeekly_StoresTheWeekdayAndClearsTheDayOfMonth()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(dayOfMonth: 15));

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Weekly(dayOfWeek: 4));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Weekly", body.GetProperty("frequency").GetString());
            Assert.Equal(4, body.GetProperty("dayOfWeek").GetInt32());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("dayOfMonth").ValueKind);
            Assert.Null(await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "day_of_month"));
        }

        [Fact]
        public async Task Edit_WithoutTitleAndEndDate_ClearsBoth()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Salary", endsOn: "2027-03-31"));

            await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(title: "  ", endsOn: null));

            Assert.Null(await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "title"));
            Assert.Null(await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "ends_on"));
        }

        [Theory]
        [InlineData(0, "MKD", "Monthly", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_AMOUNT_NOT_POSITIVE)]
        [InlineData(1_000_000_000_000, "EUR", "Monthly", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_AMOUNT_TOO_LARGE)]
        [InlineData(150, "MKD", "Monthly", 15, null, "2026-10-01", null, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP)]
        [InlineData(100_000, "USD", "Monthly", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_CURRENCY_INVALID)]
        [InlineData(100_000, "MKD", "Daily", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_FREQUENCY_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", null, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, 3, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Weekly", null, 7, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Weekly", 15, 3, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2024-10-30", null, ResultCodes.RECURRING_INCOME_START_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2027-10-06", null, ResultCodes.RECURRING_INCOME_START_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "bad", null, ResultCodes.RECURRING_INCOME_START_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2026-10-01", "2026-09-30", ResultCodes.RECURRING_INCOME_END_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2026-10-01", "2036-10-02", ResultCodes.RECURRING_INCOME_END_DATE_INVALID)]
        public async Task Edit_WithAnInvalidField_Answers400WithItsCodeAndChangesNothing(
            long amountMinor,
            string currency,
            string frequency,
            int? dayOfMonth,
            int? dayOfWeek,
            string startsOn,
            string? endsOn,
            string expectedErrorCode)
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Of(frequency, dayOfMonth, dayOfWeek, amountMinor, currency, "Salary", startsOn, endsOn));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_TitleOf61Characters_Answers400TitleInvalidAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(title: new string('a', 61)));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_TITLE_INVALID);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_UnknownIncome_Answers404NotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, Guid.CreateVersion7(), RecurringIncomeBody.Monthly());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.RECURRING_INCOME_NOT_FOUND);
        }

        [Fact]
        public async Task Edit_AStoppedIncome_Answers404NotFoundAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly());
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(5_000_000));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.RECURRING_INCOME_NOT_FOUND);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_AfterARestart_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeRequests.RestartAsync(user.Client, recurringIncomeId);

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(5_000_000));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Edit_ChangingTheStartWhileNoEntryExists_Answers200WithTheNewStart()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(startsOn: "2026-10-01"));

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(startsOn: "2026-11-01"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("2026-11-01", (await AuthRequests.ReadJsonAsync(response)).GetProperty("startsOn").GetString());
            Assert.Equal("2026-11-01", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "starts_on"));
        }

        [Fact]
        public async Task Edit_ChangingTheStartWhileAnEntryExists_Answers400StartCannotChangeAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(startsOn: "2026-10-01"));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 15));
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(5_000_000, startsOn: "2026-10-02"));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_CANNOT_CHANGE);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_ChangingTheStartWhileOnlyADeletedEntryExists_Answers400StartCannotChange()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(startsOn: "2026-10-01"));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 15), deletedAt: _app.Clock.GetUtcNow());

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(startsOn: "2026-10-02"));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_CANNOT_CHANGE);
        }

        [Fact]
        public async Task Edit_KeepingTheStartWhileAnEntryExists_Answers200AndChangesTheOtherFieldsButNotTheEntries()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(startsOn: "2026-10-01"));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 15));
            string entriesBefore = await RecurringIncomeRows.EntriesSnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(5_000_000, startsOn: "2026-10-01"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("5000000", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "amount_minor"));
            Assert.Equal(entriesBefore, await RecurringIncomeRows.EntriesSnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_ChangingTheStartWhenOnlyAnotherIncomeHasEntries_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid withEntries = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Salary"));
            Guid withoutEntries = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Rent", startsOn: "2026-10-01"));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, withEntries, new DateOnly(2026, 10, 15));

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, withoutEntries, RecurringIncomeBody.Monthly(title: "Rent", startsOn: "2026-10-03"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Edit_KeepingAStartThatIsNowOutsideTheWindow_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeRows.SetStartsOnAsync(_app, recurringIncomeId, new DateOnly(2023, 5, 4));

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(3_000_000, startsOn: "2023-05-04"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("2023-05-04", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "starts_on"));
            Assert.Equal("3000000", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "amount_minor"));
        }

        [Fact]
        public async Task Edit_ChangingAnOldStartToAnotherDateOutsideTheWindow_Answers400StartDateInvalid()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeRows.SetStartsOnAsync(_app, recurringIncomeId, new DateOnly(2023, 5, 4));

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(startsOn: "2023-05-05"));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_DATE_INVALID);
        }

        [Fact]
        public async Task Edit_AnIncomeWithEntries_LeavesEveryEntryAsItWas()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 9, 15));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 15));
            string entriesBefore = await RecurringIncomeRows.EntriesSnapshotAsync(_app);

            await RecurringIncomeRequests.EditAsync(user.Client, recurringIncomeId, RecurringIncomeBody.Monthly(9_000_000, "MKD", "Raise", 20));

            Assert.Equal(entriesBefore, await RecurringIncomeRows.EntriesSnapshotAsync(_app));
        }
    }
}
