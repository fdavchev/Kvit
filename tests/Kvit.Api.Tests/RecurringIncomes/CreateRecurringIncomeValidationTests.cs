using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CreateRecurringIncomeValidationTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(0, "MKD", "Monthly", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_AMOUNT_NOT_POSITIVE)]
        [InlineData(-100, "MKD", "Monthly", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_AMOUNT_NOT_POSITIVE)]
        [InlineData(1_000_000_000_000, "EUR", "Monthly", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_AMOUNT_TOO_LARGE)]
        [InlineData(150, "MKD", "Monthly", 15, null, "2026-10-01", null, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP)]
        [InlineData(100_000, "USD", "Monthly", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_CURRENCY_INVALID)]
        [InlineData(100_000, "mkd", "Monthly", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_CURRENCY_INVALID)]
        [InlineData(100_000, "MKD", "Daily", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_FREQUENCY_INVALID)]
        [InlineData(100_000, "MKD", "monthly", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_FREQUENCY_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", null, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 0, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 32, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, 3, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", null, 3, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Weekly", null, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Weekly", null, 7, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Weekly", null, -1, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Weekly", 15, 3, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Weekly", 15, null, "2026-10-01", null, ResultCodes.RECURRING_INCOME_DAY_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2024-10-30", null, ResultCodes.RECURRING_INCOME_START_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2027-10-06", null, ResultCodes.RECURRING_INCOME_START_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2000-01-01", null, ResultCodes.RECURRING_INCOME_START_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "bad", null, ResultCodes.RECURRING_INCOME_START_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2026-10-01T00:00:00", null, ResultCodes.RECURRING_INCOME_START_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2026-10-01", "2026-09-30", ResultCodes.RECURRING_INCOME_END_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2026-10-01", "2036-10-02", ResultCodes.RECURRING_INCOME_END_DATE_INVALID)]
        [InlineData(100_000, "MKD", "Monthly", 15, null, "2026-10-01", "bad", ResultCodes.RECURRING_INCOME_END_DATE_INVALID)]
        public async Task Create_WithAnInvalidField_Answers400WithItsCodeAndWritesNothing(
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
            RecurringIncomeBody body = RecurringIncomeBody.Of(frequency, dayOfMonth, dayOfWeek, amountMinor, currency, "Salary", startsOn, endsOn);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, body);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(0, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Create_TitleOf61Characters_Answers400TitleInvalidAndWritesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: new string('a', 61)));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_TITLE_INVALID);
            Assert.Equal(0, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Create_TitleOf60CharactersWithSpacesAround_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "  " + new string('a', 60) + "  "));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Create_SeveralInvalidFields_ReportsTheCurrencyFirst()
        {
            SignedInUser user = await _app.SignUpAsync();
            RecurringIncomeBody body = RecurringIncomeBody.Of("Daily", null, null, 0, "USD", new string('a', 61), "bad", "worse");

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, body);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_CURRENCY_INVALID);
        }

        [Fact]
        public async Task Create_InvalidTitleAndFrequencyAndStart_ReportsTheTitleFirst()
        {
            SignedInUser user = await _app.SignUpAsync();
            RecurringIncomeBody body = RecurringIncomeBody.Of("Daily", null, null, title: new string('a', 61), startsOn: "bad", endsOn: "worse");

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, body);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_TITLE_INVALID);
        }

        [Fact]
        public async Task Create_InvalidDayAndStartAndEnd_ReportsTheDayFirst()
        {
            SignedInUser user = await _app.SignUpAsync();
            RecurringIncomeBody body = RecurringIncomeBody.Monthly(dayOfMonth: 40, startsOn: "bad", endsOn: "worse");

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, body);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_DAY_INVALID);
        }

        [Fact]
        public async Task Create_InvalidStartAndEnd_ReportsTheStartFirst()
        {
            SignedInUser user = await _app.SignUpAsync();
            RecurringIncomeBody body = RecurringIncomeBody.Monthly(startsOn: "bad", endsOn: "worse");

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, body);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_DATE_INVALID);
        }
    }
}
