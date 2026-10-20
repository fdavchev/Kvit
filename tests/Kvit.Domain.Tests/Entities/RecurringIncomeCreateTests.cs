using System.Globalization;
using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class RecurringIncomeCreateTests
    {
        private const int TitleMaxLength = 60;

        [Fact]
        public void Create_ValidMonthlyInput_KeepsEveryFieldAndStartsActive()
        {
            Guid clientRequestId = Guid.CreateVersion7();
            RecurringIncomeInput input = RecurringIncomeTestData.MonthlyInput(4_500_000, "MKD", "Salary", 15, "2026-10-01", "2027-03-31");

            Result<RecurringIncome> result = RecurringIncome.Create(RecurringIncomeTestData.UserId, input, clientRequestId, RecurringIncomeTestData.UtcToday, RecurringIncomeTestData.Moment);

            Assert.True(result.IsSuccess);
            RecurringIncome income = result.Value;
            Assert.NotEqual(Guid.Empty, income.Id);
            Assert.Equal(RecurringIncomeTestData.UserId, income.UserId);
            Assert.Equal(4_500_000, income.AmountMinor);
            Assert.Equal(Currency.MKD, income.Currency);
            Assert.Equal("Salary", income.Title);
            Assert.Equal(RecurringIncomeFrequency.Monthly, income.Frequency);
            Assert.Equal(15, income.DayOfMonth);
            Assert.Null(income.DayOfWeek);
            Assert.Equal(new DateOnly(2026, 10, 1), income.StartsOn);
            Assert.Equal(new DateOnly(2027, 3, 31), income.EndsOn);
            Assert.Equal(clientRequestId, income.ClientRequestId);
            Assert.Equal(RecurringIncomeTestData.Moment, income.CreatedAt);
            Assert.Null(income.StoppedAt);
        }

        [Fact]
        public void Create_ValidWeeklyInput_KeepsTheWeekdayAndNoDayOfMonth()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.WeeklyInput(dayOfWeek: 5));

            Assert.True(result.IsSuccess);
            Assert.Equal(RecurringIncomeFrequency.Weekly, result.Value.Frequency);
            Assert.Equal(5, result.Value.DayOfWeek);
            Assert.Null(result.Value.DayOfMonth);
        }

        [Fact]
        public void Create_InEuro_KeepsTheCurrencyAndTheCents()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(123_456, "EUR"));

            Assert.Equal(Currency.EUR, income.Currency);
            Assert.Equal(123_456, income.AmountMinor);
        }

        [Fact]
        public void Create_WithoutAnEndDate_KeepsItNull()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(endsOn: null));

            Assert.Null(income.EndsOn);
        }

        [Fact]
        public void Create_TwoIncomes_GetDifferentIds()
        {
            RecurringIncome first = RecurringIncomeTestData.NewMonthly();
            RecurringIncome second = RecurringIncomeTestData.NewMonthly();

            Assert.NotEqual(first.Id, second.Id);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("   ", null)]
        [InlineData("  Salary  ", "Salary")]
        public void Create_Title_IsTrimmedAndEmptyBecomesNull(string? title, string? expected)
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(title: title));

            Assert.Equal(expected, income.Title);
        }

        [Fact]
        public void Create_TitleOf60Characters_IsAccepted()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(title: new string('a', TitleMaxLength)));

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Create_TitleOf60CharactersWithSpacesAround_IsAcceptedBecauseTheLengthCountsAfterTheTrim()
        {
            string title = "  " + new string('a', TitleMaxLength) + "  ";

            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(title: title));

            Assert.True(result.IsSuccess);
            Assert.Equal(new string('a', TitleMaxLength), result.Value.Title);
        }

        [Fact]
        public void Create_TitleOf61Characters_FailsWithTheTitleCode()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(title: new string('a', TitleMaxLength + 1)));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_TITLE_INVALID);
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("mkd")]
        [InlineData("")]
        public void Create_UnknownCurrency_FailsWithTheCurrencyCode(string currency)
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(currency: currency));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_CURRENCY_INVALID);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void Create_AmountNotAboveZero_FailsWithTheAmountNotPositiveCode(long amountMinor)
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(amountMinor));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_AMOUNT_NOT_POSITIVE);
        }

        [Fact]
        public void Create_AmountAboveTheLargestAllowed_FailsWithTheAmountTooLargeCode()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(ExpenseFields.AmountMaxMinorUnits + 1, "EUR"));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_AMOUNT_TOO_LARGE);
        }

        [Fact]
        public void Create_LargestAllowedAmountInEuro_IsAccepted()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(ExpenseFields.AmountMaxMinorUnits, "EUR"));

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Create_DenarAmountWithDeni_FailsWithTheCurrencyStepCode()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(150, "MKD"));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP);
        }

        [Fact]
        public void Create_EuroAmountWithCents_IsAccepted()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(1, "EUR"));

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("Daily")]
        [InlineData("monthly")]
        [InlineData("Yearly")]
        [InlineData("")]
        public void Create_UnknownFrequency_FailsWithTheFrequencyCode(string frequency)
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.InputOf(frequency, 15, null));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_FREQUENCY_INVALID);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(31)]
        public void Create_MonthlyDayOnTheEdgesOfTheMonth_IsAccepted(int dayOfMonth)
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(dayOfMonth: dayOfMonth));

            Assert.True(result.IsSuccess);
            Assert.Equal(dayOfMonth, result.Value.DayOfMonth);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(32)]
        [InlineData(-1)]
        [InlineData(null)]
        public void Create_MonthlyDayMissingOrOutsideOneTo31_FailsWithTheDayCode(int? dayOfMonth)
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(dayOfMonth: dayOfMonth));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_DAY_INVALID);
        }

        [Fact]
        public void Create_MonthlyWithAWeekdayAsWell_FailsWithTheDayCode()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.InputOf("Monthly", 15, 3));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_DAY_INVALID);
        }

        [Fact]
        public void Create_MonthlyWithOnlyAWeekday_FailsWithTheDayCode()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.InputOf("Monthly", null, 3));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_DAY_INVALID);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(6)]
        public void Create_WeeklyWeekdayOnTheEdgesOfTheWeek_IsAccepted(int dayOfWeek)
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.WeeklyInput(dayOfWeek: dayOfWeek));

            Assert.True(result.IsSuccess);
            Assert.Equal(dayOfWeek, result.Value.DayOfWeek);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(-1)]
        [InlineData(null)]
        public void Create_WeeklyWeekdayMissingOrOutsideZeroTo6_FailsWithTheDayCode(int? dayOfWeek)
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.WeeklyInput(dayOfWeek: dayOfWeek));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_DAY_INVALID);
        }

        [Fact]
        public void Create_WeeklyWithADayOfMonthAsWell_FailsWithTheDayCode()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.InputOf("Weekly", 15, 3));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_DAY_INVALID);
        }

        [Fact]
        public void Create_WeeklyWithOnlyADayOfMonth_FailsWithTheDayCode()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.InputOf("Weekly", 15, null));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_DAY_INVALID);
        }

        [Theory]
        [InlineData("2024-10-31")]
        [InlineData("2024-11-01")]
        [InlineData("2026-10-19")]
        [InlineData("2027-10-19")]
        public void Create_StartOnTheEdgesOfThePastWindowAndOneYearAhead_IsAccepted(string startsOn)
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(startsOn: startsOn));

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("2024-10-30")]
        [InlineData("2027-10-20")]
        [InlineData("2000-01-01")]
        [InlineData("2026-13-01")]
        [InlineData("19.10.2026")]
        [InlineData("2026-10-19T00:00:00")]
        [InlineData("")]
        public void Create_StartOutsideTheWindowOrNotADate_FailsWithTheStartDateCode(string startsOn)
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(startsOn: startsOn));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_DATE_INVALID);
        }

        [Theory]
        [InlineData("2026-03-01", "2024-03-31", "2024-03-30")]
        [InlineData("2026-01-31", "2024-01-31", "2024-01-30")]
        [InlineData("2026-12-31", "2024-12-31", "2024-12-30")]
        public void Create_EarliestStart_IsTheDayBeforeTheFirstOfTheMonth23MonthsBeforeTheUtcMonth(string utcToday, string earliestAccepted, string latestRefused)
        {
            DateOnly today = DateOnly.ParseExact(utcToday, ExpenseFields.DateFormat, CultureInfo.InvariantCulture);

            Result<RecurringIncome> accepted = RecurringIncomeTestData.CreateOn(RecurringIncomeTestData.MonthlyInput(startsOn: earliestAccepted), today);
            Result<RecurringIncome> refused = RecurringIncomeTestData.CreateOn(RecurringIncomeTestData.MonthlyInput(startsOn: latestRefused), today);

            Assert.True(accepted.IsSuccess);
            ResultAssert.Failed(refused, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_DATE_INVALID);
        }

        [Theory]
        [InlineData("2026-03-01", "2027-03-01", "2027-03-02")]
        [InlineData("2026-12-31", "2027-12-31", "2028-01-01")]
        public void Create_LatestStart_IsOneYearAfterTheUtcToday(string utcToday, string latestAccepted, string earliestRefused)
        {
            DateOnly today = DateOnly.ParseExact(utcToday, ExpenseFields.DateFormat, CultureInfo.InvariantCulture);

            Result<RecurringIncome> accepted = RecurringIncomeTestData.CreateOn(RecurringIncomeTestData.MonthlyInput(startsOn: latestAccepted), today);
            Result<RecurringIncome> refused = RecurringIncomeTestData.CreateOn(RecurringIncomeTestData.MonthlyInput(startsOn: earliestRefused), today);

            Assert.True(accepted.IsSuccess);
            ResultAssert.Failed(refused, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_DATE_INVALID);
        }

        [Fact]
        public void Create_ThePastEdgeOfTheStartWindow_IsTheSameAsForIncomeEntries()
        {
            Result<RecurringIncome> recurringEarliest = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(startsOn: "2024-10-31"));
            Result<IncomeFields> entryEarliest = IncomeFields.Create(PersonalEntryTestData.IncomeInputOf(entryDate: "2024-10-31"), RecurringIncomeTestData.UtcToday);
            Result<RecurringIncome> recurringTooEarly = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(startsOn: "2024-10-30"));
            Result<IncomeFields> entryTooEarly = IncomeFields.Create(PersonalEntryTestData.IncomeInputOf(entryDate: "2024-10-30"), RecurringIncomeTestData.UtcToday);

            Assert.Equal(entryEarliest.IsSuccess, recurringEarliest.IsSuccess);
            Assert.Equal(entryTooEarly.IsSuccess, recurringTooEarly.IsSuccess);
        }

        [Fact]
        public void Create_EndOnTheStartDay_IsAccepted()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01", endsOn: "2026-10-01"));

            Assert.True(result.IsSuccess);
            Assert.Equal(new DateOnly(2026, 10, 1), result.Value.EndsOn);
        }

        [Fact]
        public void Create_EndExactlyTenYearsAfterTheStart_IsAccepted()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01", endsOn: "2036-10-01"));

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Create_EndOneDayAfterTenYearsFromTheStart_FailsWithTheEndDateCode()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01", endsOn: "2036-10-02"));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_END_DATE_INVALID);
        }

        [Fact]
        public void Create_EndOneDayBeforeTheStart_FailsWithTheEndDateCode()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01", endsOn: "2026-09-30"));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_END_DATE_INVALID);
        }

        [Theory]
        [InlineData("bad")]
        [InlineData("2027-13-01")]
        [InlineData("01.03.2027")]
        public void Create_EndNotADate_FailsWithTheEndDateCode(string endsOn)
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(endsOn: endsOn));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_END_DATE_INVALID);
        }

        [Fact]
        public void Create_StartInThePastWithAnEndDateAlsoInThePast_IsAccepted()
        {
            Result<RecurringIncome> result = RecurringIncomeTestData.Create(RecurringIncomeTestData.MonthlyInput(startsOn: "2025-01-01", endsOn: "2025-06-30"));

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Create_EverythingInvalid_ReportsTheCurrencyFirst()
        {
            RecurringIncomeInput input = RecurringIncomeTestData.InputOf("Daily", null, null, 0, "USD", new string('a', 61), "bad", "worse");

            ResultAssert.Failed(RecurringIncomeTestData.Create(input), HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_CURRENCY_INVALID);
        }

        [Fact]
        public void Create_AmountAndTitleAndFrequencyInvalid_ReportsTheAmountBeforeTheTitle()
        {
            RecurringIncomeInput input = RecurringIncomeTestData.InputOf("Daily", null, null, 0, "MKD", new string('a', 61), "bad", "worse");

            ResultAssert.Failed(RecurringIncomeTestData.Create(input), HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_AMOUNT_NOT_POSITIVE);
        }

        [Fact]
        public void Create_TitleAndFrequencyAndStartInvalid_ReportsTheTitleBeforeTheFrequency()
        {
            RecurringIncomeInput input = RecurringIncomeTestData.InputOf("Daily", null, null, 4_500_000, "MKD", new string('a', 61), "bad", "worse");

            ResultAssert.Failed(RecurringIncomeTestData.Create(input), HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_TITLE_INVALID);
        }

        [Fact]
        public void Create_FrequencyAndStartInvalid_ReportsTheFrequencyBeforeTheStart()
        {
            RecurringIncomeInput input = RecurringIncomeTestData.InputOf("Daily", 15, null, startsOn: "bad", endsOn: "worse");

            ResultAssert.Failed(RecurringIncomeTestData.Create(input), HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_FREQUENCY_INVALID);
        }

        [Fact]
        public void Create_DayAndStartInvalid_ReportsTheDayBeforeTheStart()
        {
            RecurringIncomeInput input = RecurringIncomeTestData.MonthlyInput(dayOfMonth: 40, startsOn: "bad", endsOn: "worse");

            ResultAssert.Failed(RecurringIncomeTestData.Create(input), HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_DAY_INVALID);
        }

        [Fact]
        public void Create_StartAndEndInvalid_ReportsTheStartBeforeTheEnd()
        {
            RecurringIncomeInput input = RecurringIncomeTestData.MonthlyInput(startsOn: "bad", endsOn: "worse");

            ResultAssert.Failed(RecurringIncomeTestData.Create(input), HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_DATE_INVALID);
        }
    }
}
