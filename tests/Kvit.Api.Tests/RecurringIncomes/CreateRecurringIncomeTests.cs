using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CreateRecurringIncomeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Create_MonthlyIncome_Answers200WithTheSavedRow()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(4_500_000, "MKD", "  Salary  ", 15, "2026-10-01", "2027-03-31"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(RecurringIncomeRequests.RowFieldNames, body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
            Assert.Equal(4_500_000, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal("Salary", body.GetProperty("title").GetString());
            Assert.Equal("Monthly", body.GetProperty("frequency").GetString());
            Assert.Equal(15, body.GetProperty("dayOfMonth").GetInt32());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("dayOfWeek").ValueKind);
            Assert.Equal("2026-10-01", body.GetProperty("startsOn").GetString());
            Assert.Equal("2027-03-31", body.GetProperty("endsOn").GetString());
            Assert.Equal(ExpensesApp.ClockStart, body.GetProperty("createdAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Create_WeeklyIncome_Answers200WithTheWeekdayAndANullDayOfMonth()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Weekly(dayOfWeek: 0));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Weekly", body.GetProperty("frequency").GetString());
            Assert.Equal(0, body.GetProperty("dayOfWeek").GetInt32());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("dayOfMonth").ValueKind);
        }

        [Fact]
        public async Task Create_IncomeInEuro_AnswersTheCents()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(123_456, "EUR")));

            Assert.Equal("EUR", body.GetProperty("currency").GetString());
            Assert.Equal(123_456, body.GetProperty("amountMinor").GetInt64());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_WithoutATitleOrWithABlankOne_AnswersANullTitleAndStoresNull(string? title)
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: title)));

            Assert.Equal(JsonValueKind.Null, body.GetProperty("title").ValueKind);
            Assert.Null(await RecurringIncomeRows.ValueAsync(_app, body.GetProperty("id").GetGuid(), "title"));
        }

        [Fact]
        public async Task Create_WithoutAnEndDate_AnswersANullEndDate()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(endsOn: null)));

            Assert.Equal(JsonValueKind.Null, body.GetProperty("endsOn").ValueKind);
        }

        [Fact]
        public async Task Create_Income_StoresTheRowForTheCallerAsActiveWithTheCreateMoment()
        {
            SignedInUser user = await _app.SignUpAsync();

            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(4_500_000, "MKD", "Salary", 15, "2026-10-01", "2027-03-31"));

            Assert.Equal(user.UserId.ToString(), await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "user_id"));
            Assert.Equal("4500000", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "amount_minor"));
            Assert.Equal("MKD", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "currency"));
            Assert.Equal("Salary", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "title"));
            Assert.Equal("Monthly", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "frequency"));
            Assert.Equal("15", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "day_of_month"));
            Assert.Null(await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "day_of_week"));
            Assert.Equal("2026-10-01", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "starts_on"));
            Assert.Equal("2027-03-31", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "ends_on"));
            Assert.Null(await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "stopped_at"));
            Assert.Equal(_app.Clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "created_at AT TIME ZONE 'UTC'"));
        }

        [Fact]
        public async Task Create_Income_WritesNoPersonalEntryAndNoActivityOrUsageEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            int eventsBefore = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());

            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
            Assert.Equal(eventsBefore, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }

        [Theory]
        [InlineData("2024-10-31")]
        [InlineData("2026-10-05")]
        [InlineData("2027-10-05")]
        public async Task Create_StartOnTheEdgesOfTheWindowUpToOneYearAhead_Answers200(string startsOn)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(startsOn: startsOn));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Create_EndOnTheStartDayAndExactlyTenYearsAfterIt_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage sameDay = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(startsOn: "2026-10-01", endsOn: "2026-10-01"));
            HttpResponseMessage tenYears = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(startsOn: "2026-10-01", endsOn: "2036-10-01"));

            Assert.Equal(HttpStatusCode.OK, sameDay.StatusCode);
            Assert.Equal(HttpStatusCode.OK, tenYears.StatusCode);
        }

        [Fact]
        public async Task Create_WithoutAClientRequestId_Answers400AndWritesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(), Guid.Empty);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Create_TwoIncomesOfOneUser_SaveTwoRowsWithDifferentIds()
        {
            SignedInUser user = await _app.SignUpAsync();

            Guid first = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Salary"));
            Guid second = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Weekly(title: "Rent"));

            Assert.NotEqual(first, second);
            Assert.Equal(2, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
        }
    }
}
