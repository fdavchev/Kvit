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
    public class IncomeFieldsTests
    {
        [Fact]
        public void Create_ValidInput_KeepsTheAmountCurrencyAndDate()
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(1_550, "EUR", "2026-10-18"));

            Assert.True(fields.IsSuccess);
            Assert.Equal(1_550, fields.Value.Amount.MinorUnits);
            Assert.Equal(Currency.EUR, fields.Value.Amount.Currency);
            Assert.Equal(new DateOnly(2026, 10, 18), fields.Value.EntryDate);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("   ", null)]
        [InlineData("  Salary  ", "Salary")]
        public void Create_Note_IsTrimmedAndEmptyBecomesNull(string? note, string? expected)
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(note: note));

            Assert.Equal(expected, fields.Value.Note);
        }

        [Fact]
        public void Create_NoteOf500Characters_IsAccepted()
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(note: new string('a', ExpenseFields.NoteMaxLength)));

            Assert.True(fields.IsSuccess);
        }

        [Fact]
        public void Create_NoteOf501Characters_FailsWithThePersonalEntryNoteCode()
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(note: new string('a', ExpenseFields.NoteMaxLength + 1)));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_NOTE_INVALID);
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("mkd")]
        [InlineData("")]
        public void Create_UnknownCurrency_FailsWithThePersonalEntryCurrencyCode(string currency)
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(currency: currency));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CURRENCY_INVALID);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void Create_AmountNotAboveZero_FailsWithTheAmountNotPositiveCode(long amountMinor)
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(amountMinor));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE);
        }

        [Fact]
        public void Create_AmountAboveTheLargestAllowed_FailsWithTheAmountTooLargeCode()
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(ExpenseFields.AmountMaxMinorUnits + 1, "EUR"));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_AMOUNT_TOO_LARGE);
        }

        [Fact]
        public void Create_DenarAmountWithDeni_FailsWithTheCurrencyStepCode()
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(150, "MKD"));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP);
        }

        [Fact]
        public void Create_EuroAmountWithCents_IsAccepted()
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(1, "EUR"));

            Assert.True(fields.IsSuccess);
        }

        [Theory]
        [InlineData("2024-10-31")]
        [InlineData("2024-11-01")]
        [InlineData("2026-10-19")]
        [InlineData("2026-10-20")]
        public void Create_DateOnTheEdgesOfTheLast24MonthsUpToTomorrowInUtc_IsAccepted(string entryDate)
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(entryDate: entryDate));

            Assert.True(fields.IsSuccess);
        }

        [Theory]
        [InlineData("2024-10-30")]
        [InlineData("2026-10-21")]
        [InlineData("2000-01-01")]
        [InlineData("1999-12-31")]
        [InlineData("2027-10-20")]
        [InlineData("2026-13-01")]
        [InlineData("18.10.2026")]
        [InlineData("2026-10-18T00:00:00")]
        [InlineData("")]
        public void Create_DateOutsideTheRangeOrNotADate_FailsWithThePersonalEntryDateCode(string entryDate)
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(entryDate: entryDate));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
        }

        [Theory]
        [InlineData("2026-03-01", "2024-03-31", "2024-03-30")]
        [InlineData("2026-01-31", "2024-01-31", "2024-01-30")]
        [InlineData("2026-12-31", "2024-12-31", "2024-12-30")]
        public void Create_EarliestDate_IsTheDayBeforeTheFirstOfTheMonth23MonthsBeforeTheUtcMonth(string utcToday, string earliestAccepted, string latestRefused)
        {
            DateOnly today = DateOnly.ParseExact(utcToday, ExpenseFields.DateFormat, CultureInfo.InvariantCulture);

            Result<IncomeFields> accepted = IncomeFields.Create(PersonalEntryTestData.IncomeInputOf(entryDate: earliestAccepted), today);
            Result<IncomeFields> refused = IncomeFields.Create(PersonalEntryTestData.IncomeInputOf(entryDate: latestRefused), today);

            Assert.True(accepted.IsSuccess);
            ResultAssert.Failed(refused, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
        }

        [Fact]
        public void Create_TheRangeIsTheSameAsForSpending()
        {
            Result<PersonalEntryFields> spendingEarliest = PersonalEntryFields.Create(PersonalEntryTestData.Input(Guid.CreateVersion7(), entryDate: "2024-10-31"), PersonalEntryTestData.UtcToday);
            Result<IncomeFields> incomeEarliest = Create(PersonalEntryTestData.IncomeInputOf(entryDate: "2024-10-31"));
            Result<PersonalEntryFields> spendingTooEarly = PersonalEntryFields.Create(PersonalEntryTestData.Input(Guid.CreateVersion7(), entryDate: "2024-10-30"), PersonalEntryTestData.UtcToday);
            Result<IncomeFields> incomeTooEarly = Create(PersonalEntryTestData.IncomeInputOf(entryDate: "2024-10-30"));

            Assert.Equal(spendingEarliest.IsSuccess, incomeEarliest.IsSuccess);
            Assert.Equal(spendingTooEarly.IsSuccess, incomeTooEarly.IsSuccess);
        }

        [Fact]
        public void CreateForEdit_KeepingADateThatIsNowOutsideTheRange_IsAccepted()
        {
            DateOnly keptDate = new(2023, 5, 4);

            Result<IncomeFields> fields = IncomeFields.CreateForEdit(PersonalEntryTestData.IncomeInputOf(entryDate: "2023-05-04"), PersonalEntryTestData.UtcToday, keptDate);

            Assert.True(fields.IsSuccess);
            Assert.Equal(keptDate, fields.Value.EntryDate);
        }

        [Fact]
        public void CreateForEdit_ChangingTheDateToAnotherDateOutsideTheRange_FailsWithThePersonalEntryDateCode()
        {
            Result<IncomeFields> fields = IncomeFields.CreateForEdit(PersonalEntryTestData.IncomeInputOf(entryDate: "2023-05-05"), PersonalEntryTestData.UtcToday, new DateOnly(2023, 5, 4));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
        }

        [Theory]
        [InlineData("2024-10-31")]
        [InlineData("2026-10-20")]
        public void CreateForEdit_ChangingTheDateToTheEdgesOfTheRange_IsAccepted(string entryDate)
        {
            Result<IncomeFields> fields = IncomeFields.CreateForEdit(PersonalEntryTestData.IncomeInputOf(entryDate: entryDate), PersonalEntryTestData.UtcToday, new DateOnly(2023, 5, 4));

            Assert.True(fields.IsSuccess);
        }

        [Theory]
        [InlineData("2024-10-30")]
        [InlineData("2026-10-21")]
        public void CreateForEdit_ChangingARecentDateToJustOutsideTheRange_FailsWithThePersonalEntryDateCode(string entryDate)
        {
            Result<IncomeFields> fields = IncomeFields.CreateForEdit(PersonalEntryTestData.IncomeInputOf(entryDate: entryDate), PersonalEntryTestData.UtcToday, new DateOnly(2026, 10, 18));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
        }

        [Fact]
        public void Create_SeveralInvalidFields_ReportsTheCurrencyFirstLikeSpending()
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(0, "USD", "bad", new string('a', 501)));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CURRENCY_INVALID);
        }

        [Fact]
        public void Create_InvalidAmountAndDate_ReportsTheAmountBeforeTheDate()
        {
            Result<IncomeFields> fields = Create(PersonalEntryTestData.IncomeInputOf(0, "MKD", "bad"));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE);
        }

        private static Result<IncomeFields> Create(IncomeInput input)
        {
            return IncomeFields.Create(input, PersonalEntryTestData.UtcToday);
        }
    }
}
