using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class PersonalEntryFieldsTests
    {
        private static readonly Guid CategoryId = Guid.CreateVersion7();

        [Fact]
        public void Create_ValidInput_KeepsTheCategoryAmountCurrencyAndDate()
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, 1_550, "EUR", "2026-10-18"));

            Assert.True(fields.IsSuccess);
            Assert.Equal(CategoryId, fields.Value.CategoryId);
            Assert.Equal(1_550, fields.Value.Amount.MinorUnits);
            Assert.Equal(Currency.EUR, fields.Value.Amount.Currency);
            Assert.Equal(new DateOnly(2026, 10, 18), fields.Value.EntryDate);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("   ", null)]
        [InlineData("  Coffee with Ana  ", "Coffee with Ana")]
        public void Create_Note_IsTrimmedAndEmptyBecomesNull(string? note, string? expected)
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, note: note));

            Assert.Equal(expected, fields.Value.Note);
        }

        [Fact]
        public void Create_NoteOf500Characters_IsAccepted()
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, note: new string('a', ExpenseFields.NoteMaxLength)));

            Assert.True(fields.IsSuccess);
        }

        [Fact]
        public void Create_NoteOf501Characters_FailsWithTheExpenseNoteCode()
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, note: new string('a', ExpenseFields.NoteMaxLength + 1)));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_NOTE_INVALID);
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("mkd")]
        [InlineData("")]
        public void Create_UnknownCurrency_FailsWithTheExpenseCurrencyCode(string currency)
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, currency: currency));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CURRENCY_INVALID);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void Create_AmountNotAboveZero_FailsWithTheAmountNotPositiveCode(long amountMinor)
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, amountMinor));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE);
        }

        [Fact]
        public void Create_AmountAboveTheLargestAllowed_FailsWithTheAmountTooLargeCode()
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, ExpenseFields.AmountMaxMinorUnits + 1, "EUR"));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_AMOUNT_TOO_LARGE);
        }

        [Fact]
        public void Create_DenarAmountWithDeni_FailsWithTheCurrencyStepCode()
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, 150, "MKD"));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP);
        }

        [Fact]
        public void Create_EuroAmountWithCents_IsAccepted()
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, 1, "EUR"));

            Assert.True(fields.IsSuccess);
        }

        [Theory]
        [InlineData("2000-01-01")]
        [InlineData("2027-10-20")]
        public void Create_DateOnTheEdgesOfTheAllowedRange_IsAccepted(string entryDate)
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, entryDate: entryDate));

            Assert.True(fields.IsSuccess);
        }

        [Theory]
        [InlineData("1999-12-31")]
        [InlineData("2027-10-21")]
        [InlineData("2026-13-01")]
        [InlineData("18.10.2026")]
        [InlineData("2026-10-18T00:00:00")]
        [InlineData("")]
        public void Create_DateOutsideTheRangeOrNotADate_FailsWithThePersonalEntryDateCode(string entryDate)
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, entryDate: entryDate));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
        }

        [Fact]
        public void Create_SeveralInvalidFields_ReportsTheCurrencyFirstLikeExpenses()
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, 0, "USD", "bad", new string('a', 501)));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CURRENCY_INVALID);
        }

        [Fact]
        public void Create_InvalidAmountAndDate_ReportsTheAmountBeforeTheDate()
        {
            Result<PersonalEntryFields> fields = Create(PersonalEntryTestData.Input(CategoryId, 0, "MKD", "bad"));

            ResultAssert.Failed(fields, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE);
        }

        private static Result<PersonalEntryFields> Create(PersonalEntryInput input)
        {
            return PersonalEntryFields.Create(input, PersonalEntryTestData.UtcToday);
        }
    }
}
