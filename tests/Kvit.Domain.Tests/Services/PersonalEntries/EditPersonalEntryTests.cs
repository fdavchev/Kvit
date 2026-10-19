using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Kvit.Domain.Services.PersonalEntries;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Services.PersonalEntries
{
    public class EditPersonalEntryTests
    {
        private static readonly DateTimeOffset Later = PersonalEntryTestData.Moment.AddHours(3);

        private readonly FakePersonalEntryRepository _entries = new();
        private readonly FakeCategoryRepository _categories = new();
        private readonly EditPersonalEntry _editPersonalEntry;
        private readonly Category _pets = CategoryTestData.NewCustom("Pets", 11);
        private readonly Category _kids = CategoryTestData.NewCustom("Kids", 12);
        private readonly PersonalEntry _entry;

        public EditPersonalEntryTests()
        {
            _categories.Seed(_pets, _kids);
            _entry = PersonalEntryTestData.NewSpending(_pets.Id);
            _entries.Seed(_entry);
            _editPersonalEntry = new EditPersonalEntry(_entries, _categories, new FixedTimeProvider(Later));
        }

        [Fact]
        public async Task Execute_OwnEntryWithValidInput_ReplacesTheFieldsAndStoresTheUpdateMoment()
        {
            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_kids.Id, 250_000, "MKD", "2026-10-17", "School"));

            Assert.True(result.IsSuccess);
            Assert.Equal(_kids.Id, _entry.CategoryId);
            Assert.Equal(250_000, _entry.AmountMinor);
            Assert.Equal(new DateOnly(2026, 10, 17), _entry.EntryDate);
            Assert.Equal("School", _entry.Note);
            Assert.Equal(Later, _entry.UpdatedAt);
        }

        [Fact]
        public async Task Execute_SameCurrency_KeepsTheSavedRate()
        {
            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_pets.Id, 200_000), PersonalEntryTestData.NewerRate);

            Assert.True(result.IsSuccess);
            Assert.Equal(PersonalEntryTestData.Rate.MkdPerEur, _entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.Rate.RateDate, _entry.RateDate);
        }

        [Fact]
        public async Task Execute_CurrencyChanged_SavesTheCurrentRate()
        {
            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_pets.Id, 1_000, "EUR"), PersonalEntryTestData.NewerRate);

            Assert.True(result.IsSuccess);
            Assert.Equal(Currency.EUR, _entry.Currency);
            Assert.Equal(PersonalEntryTestData.NewerRate.MkdPerEur, _entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.NewerRate.RateDate, _entry.RateDate);
        }

        [Fact]
        public async Task Execute_CurrencyChangedWithoutACurrentRate_ThrowsBecauseTheCurrencyChangedMeanwhile()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_pets.Id, 1_000, "EUR")));
        }

        [Fact]
        public async Task Execute_KeepsItsCategoryThatWasArchivedSince_Succeeds()
        {
            _pets.Archive(PersonalEntryTestData.Moment);

            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_pets.Id, 300_000));

            Assert.True(result.IsSuccess);
            Assert.Equal(_pets.Id, _entry.CategoryId);
            Assert.Equal(300_000, _entry.AmountMinor);
        }

        [Fact]
        public async Task Execute_SwitchesToAnArchivedCategory_FailsWithCategoryInvalidAndChangesNothing()
        {
            _kids.Archive(PersonalEntryTestData.Moment);

            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_kids.Id, 300_000));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            AssertEntryUnchanged();
        }

        [Fact]
        public async Task Execute_SwitchesToTheCategoryOfAnotherUser_FailsWithCategoryInvalidAndChangesNothing()
        {
            Category others = Category.CreateCustom(PersonalEntryTestData.OtherUserId, CategoryTestData.Fields("Others"), 11);
            _categories.Seed(others);

            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(others.Id));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            AssertEntryUnchanged();
        }

        [Fact]
        public async Task Execute_EntryOfAnotherUser_FailsWith404NotFoundAndChangesNothing()
        {
            Result result = await _editPersonalEntry.Execute(PersonalEntryTestData.OtherUserId, _entry.Id, PersonalEntryTestData.Input(_pets.Id, 300_000), null, TestContext.Current.CancellationToken);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            AssertEntryUnchanged();
        }

        [Fact]
        public async Task Execute_DeletedEntry_FailsWith404NotFound()
        {
            _entry.Delete(PersonalEntryTestData.Moment);

            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_pets.Id, 300_000));

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        [Fact]
        public async Task Execute_InvalidField_FailsWithItsCodeAndChangesNothing()
        {
            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_pets.Id, 0));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE);
            AssertEntryUnchanged();
        }

        [Fact]
        public async Task Execute_UnknownEntryWithInvalidInput_ReportsNotFoundFirst()
        {
            Result result = await ExecuteAsync(Guid.CreateVersion7(), PersonalEntryTestData.Input(Guid.CreateVersion7(), 0, "USD"));

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        private void AssertEntryUnchanged()
        {
            Assert.Equal(_pets.Id, _entry.CategoryId);
            Assert.Equal(PersonalEntryTestData.OneThousandMkd, _entry.AmountMinor);
            Assert.Null(_entry.UpdatedAt);
        }

        private Task<Result> ExecuteAsync(Guid entryId, PersonalEntryInput input, ExchangeRateSnapshot? currentRate = null)
        {
            return _editPersonalEntry.Execute(PersonalEntryTestData.UserId, entryId, input, currentRate, TestContext.Current.CancellationToken);
        }
    }
}
