using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class RecurringIncomeUpdateTests
    {
        private const bool HasNoEntries = false;
        private const bool HasEntries = true;

        [Fact]
        public void Update_NewValidFields_ReplacesTheFieldsAndKeepsTheIdentity()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(endsOn: "2027-03-31"));
            Guid id = income.Id;
            Guid clientRequestId = income.ClientRequestId;
            RecurringIncomeInput input = RecurringIncomeTestData.MonthlyInput(5_000_000, "EUR", "Raise", 20, "2026-10-01", "2028-01-31");

            Result<RecurringIncome> result = income.Update(input, HasEntries, RecurringIncomeTestData.UtcToday);

            Assert.True(result.IsSuccess);
            Assert.Same(income, result.Value);
            Assert.Equal(id, income.Id);
            Assert.Equal(clientRequestId, income.ClientRequestId);
            Assert.Equal(RecurringIncomeTestData.UserId, income.UserId);
            Assert.Equal(RecurringIncomeTestData.Moment, income.CreatedAt);
            Assert.Null(income.StoppedAt);
            Assert.Equal(5_000_000, income.AmountMinor);
            Assert.Equal(Currency.EUR, income.Currency);
            Assert.Equal("Raise", income.Title);
            Assert.Equal(20, income.DayOfMonth);
            Assert.Equal(new DateOnly(2028, 1, 31), income.EndsOn);
        }

        [Fact]
        public void Update_MonthlyToWeekly_ReplacesTheDayOfMonthWithTheWeekday()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(dayOfMonth: 15));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.WeeklyInput(dayOfWeek: 4), HasEntries, RecurringIncomeTestData.UtcToday);

            Assert.True(result.IsSuccess);
            Assert.Equal(RecurringIncomeFrequency.Weekly, income.Frequency);
            Assert.Equal(4, income.DayOfWeek);
            Assert.Null(income.DayOfMonth);
        }

        [Fact]
        public void Update_WeeklyToMonthly_ReplacesTheWeekdayWithTheDayOfMonth()
        {
            RecurringIncome income = RecurringIncomeTestData.NewWeekly(RecurringIncomeTestData.WeeklyInput(dayOfWeek: 2));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(dayOfMonth: 31), HasEntries, RecurringIncomeTestData.UtcToday);

            Assert.True(result.IsSuccess);
            Assert.Equal(RecurringIncomeFrequency.Monthly, income.Frequency);
            Assert.Equal(31, income.DayOfMonth);
            Assert.Null(income.DayOfWeek);
        }

        [Fact]
        public void Update_WithoutTitleAndEndDate_ClearsBoth()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(title: "Salary", endsOn: "2027-03-31"));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(title: "  ", endsOn: null), HasEntries, RecurringIncomeTestData.UtcToday);

            Assert.True(result.IsSuccess);
            Assert.Null(income.Title);
            Assert.Null(income.EndsOn);
        }

        [Fact]
        public void Update_TitleOf61Characters_FailsWithTheTitleCodeAndChangesNothing()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(title: new string('a', 61)), HasEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_TITLE_INVALID);
            Assert.Equal("Salary", income.Title);
        }

        [Theory]
        [InlineData(0, "MKD", ResultCodes.RECURRING_INCOME_AMOUNT_NOT_POSITIVE)]
        [InlineData(1_000_000_000_000, "EUR", ResultCodes.RECURRING_INCOME_AMOUNT_TOO_LARGE)]
        [InlineData(150, "MKD", ResultCodes.MONEY_NOT_ON_CURRENCY_STEP)]
        [InlineData(100_000, "USD", ResultCodes.RECURRING_INCOME_CURRENCY_INVALID)]
        public void Update_InvalidAmountOrCurrency_FailsWithItsCodeAndChangesNothing(long amountMinor, string currency, string expectedErrorCode)
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(amountMinor, currency, "Other"), HasEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(RecurringIncomeTestData.Salary, income.AmountMinor);
            Assert.Equal(Currency.MKD, income.Currency);
            Assert.Equal("Salary", income.Title);
        }

        [Fact]
        public void Update_UnknownFrequency_FailsWithTheFrequencyCode()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.InputOf("Daily", 15, null), HasEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_FREQUENCY_INVALID);
        }

        [Fact]
        public void Update_MonthlyWithoutADay_FailsWithTheDayCodeAndKeepsTheOldDay()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(dayOfMonth: 15));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(dayOfMonth: null), HasEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_DAY_INVALID);
            Assert.Equal(15, income.DayOfMonth);
        }

        [Fact]
        public void Update_WeeklyWithADayOfMonth_FailsWithTheDayCode()
        {
            RecurringIncome income = RecurringIncomeTestData.NewWeekly();

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.InputOf("Weekly", 15, 3), HasEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_DAY_INVALID);
        }

        [Fact]
        public void Update_ChangingTheStartWhileThereAreNoEntries_StoresTheNewStart()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01"));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-11-01"), HasNoEntries, RecurringIncomeTestData.UtcToday);

            Assert.True(result.IsSuccess);
            Assert.Equal(new DateOnly(2026, 11, 1), income.StartsOn);
        }

        [Theory]
        [InlineData("2024-10-31")]
        [InlineData("2027-10-19")]
        public void Update_ChangingTheStartToTheEdgesOfTheWindowWhileThereAreNoEntries_IsAccepted(string startsOn)
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(startsOn: startsOn), HasNoEntries, RecurringIncomeTestData.UtcToday);

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("2024-10-30")]
        [InlineData("2027-10-20")]
        [InlineData("bad")]
        public void Update_ChangingTheStartOutsideTheWindowWhileThereAreNoEntries_FailsWithTheStartDateCodeAndKeepsTheOldStart(string startsOn)
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01"));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(startsOn: startsOn), HasNoEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_DATE_INVALID);
            Assert.Equal(new DateOnly(2026, 10, 1), income.StartsOn);
        }

        [Fact]
        public void Update_ChangingTheStartWhileAnEntryExists_FailsWithStartCannotChangeAndChangesNothing()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01"));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(5_000_000, startsOn: "2026-10-02"), HasEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_CANNOT_CHANGE);
            Assert.Equal(new DateOnly(2026, 10, 1), income.StartsOn);
            Assert.Equal(RecurringIncomeTestData.Salary, income.AmountMinor);
        }

        [Fact]
        public void Update_KeepingTheStartWhileAnEntryExists_IsAcceptedAndStoresTheOtherChanges()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01"));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(5_000_000, startsOn: "2026-10-01"), HasEntries, RecurringIncomeTestData.UtcToday);

            Assert.True(result.IsSuccess);
            Assert.Equal(5_000_000, income.AmountMinor);
            Assert.Equal(new DateOnly(2026, 10, 1), income.StartsOn);
        }

        [Theory]
        [InlineData(HasNoEntries)]
        [InlineData(HasEntries)]
        public void Update_KeepingAStartThatIsNowOutsideTheWindow_IsAcceptedWithOrWithoutEntries(bool hasEntries)
        {
            DateOnly longAgo = new(2023, 6, 1);
            RecurringIncome income = RecurringIncomeTestData.CreateOn(RecurringIncomeTestData.MonthlyInput(startsOn: "2023-05-04"), longAgo).Value;

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(3_000_000, startsOn: "2023-05-04"), hasEntries, RecurringIncomeTestData.UtcToday);

            Assert.True(result.IsSuccess);
            Assert.Equal(new DateOnly(2023, 5, 4), income.StartsOn);
            Assert.Equal(3_000_000, income.AmountMinor);
        }

        [Fact]
        public void Update_ChangingAnOldStartToAnotherDateOutsideTheWindow_FailsWithTheStartDateCode()
        {
            DateOnly longAgo = new(2023, 6, 1);
            RecurringIncome income = RecurringIncomeTestData.CreateOn(RecurringIncomeTestData.MonthlyInput(startsOn: "2023-05-04"), longAgo).Value;

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(startsOn: "2023-05-05"), HasNoEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_START_DATE_INVALID);
        }

        [Fact]
        public void Update_EndBeforeTheKeptStart_FailsWithTheEndDateCodeAndKeepsTheOldEnd()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01", endsOn: "2027-03-31"));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01", endsOn: "2026-09-30"), HasEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_END_DATE_INVALID);
            Assert.Equal(new DateOnly(2027, 3, 31), income.EndsOn);
        }

        [Fact]
        public void Update_EndBeforeTheNewStart_FailsWithTheEndDateCode()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01"));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-11-01", endsOn: "2026-10-15"), HasNoEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_END_DATE_INVALID);
            Assert.Equal(new DateOnly(2026, 10, 1), income.StartsOn);
        }

        [Fact]
        public void Update_EndMoreThanTenYearsAfterTheStart_FailsWithTheEndDateCode()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01"));

            Result<RecurringIncome> result = income.Update(RecurringIncomeTestData.MonthlyInput(startsOn: "2026-10-01", endsOn: "2036-10-02"), HasEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_END_DATE_INVALID);
        }

        [Fact]
        public void Update_SeveralInvalidFields_ReportsTheCurrencyFirst()
        {
            RecurringIncome income = RecurringIncomeTestData.NewMonthly();
            RecurringIncomeInput input = RecurringIncomeTestData.InputOf("Daily", null, null, 0, "USD", new string('a', 61), "bad", "worse");

            Result<RecurringIncome> result = income.Update(input, HasEntries, RecurringIncomeTestData.UtcToday);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_CURRENCY_INVALID);
        }
    }
}
