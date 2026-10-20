using System.Net;
using Kvit.Domain.Entities;
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
        private readonly FakeExchangeRateRepository _rates = new();
        private readonly FakeExchangeRateSource _rateSource = new();
        private readonly EditPersonalEntry _editPersonalEntry;
        private readonly Category _pets = CategoryTestData.NewCustom("Pets", 11);
        private readonly Category _kids = CategoryTestData.NewCustom("Kids", 12);
        private readonly PersonalEntry _entry;

        public EditPersonalEntryTests()
        {
            _categories.Seed(_pets, _kids);
            _entry = PersonalEntryTestData.NewSpending(_pets.Id);
            _entries.Seed(_entry);
            PersonalEntryTestData.SeedRate(_rates, PersonalEntryTestData.Rate);
            FixedTimeProvider timeProvider = new(Later);
            _editPersonalEntry = new EditPersonalEntry(_entries, _categories, PersonalEntryTestData.CurrentRateFrom(_rates, _rateSource, timeProvider), timeProvider);
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
            PersonalEntryTestData.SeedRate(_rates, PersonalEntryTestData.NewerRate);

            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_pets.Id, 200_000));

            Assert.True(result.IsSuccess);
            Assert.Equal(PersonalEntryTestData.Rate.MkdPerEur, _entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.Rate.RateDate, _entry.RateDate);
        }

        [Fact]
        public async Task Execute_SameCurrencyWhileTheNewestRateIsStale_NeverAsksTheSource()
        {
            SeedStaleRate();

            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_pets.Id, 200_000));

            Assert.True(result.IsSuccess);
            Assert.Empty(_rateSource.RequestedDates);
        }

        [Fact]
        public async Task Execute_CurrencyChanged_SavesTheCurrentRate()
        {
            PersonalEntryTestData.SeedRate(_rates, PersonalEntryTestData.NewerRate);

            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_pets.Id, 1_000, "EUR"));

            Assert.True(result.IsSuccess);
            Assert.Equal(Currency.EUR, _entry.Currency);
            Assert.Equal(PersonalEntryTestData.NewerRate.MkdPerEur, _entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.NewerRate.RateDate, _entry.RateDate);
        }

        [Fact]
        public async Task Execute_CurrencyChangedWhileTheNewestRateIsStale_AsksTheSourceAndSavesItsRate()
        {
            SeedStaleRate();
            _rateSource.AnswerWith(PersonalEntryTestData.UtcToday, 61.9000m);

            Result result = await ExecuteAsync(_entry.Id, PersonalEntryTestData.Input(_pets.Id, 1_000, "EUR"));

            Assert.True(result.IsSuccess);
            Assert.Equal([PersonalEntryTestData.UtcToday], _rateSource.RequestedDates);
            Assert.Equal(61.9000m, _entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.UtcToday, _entry.RateDate);
        }

        [Fact]
        public async Task Execute_EntryWhoseCurrencyWasChangedByAnotherRequestMeanwhile_SavesTheCurrentRateForTheRequestedCurrency()
        {
            PersonalEntry changedMeanwhile = PersonalEntryTestData.NewSpending(_pets.Id, currency: "EUR", amountMinor: 1_000);
            _entries.Seed(changedMeanwhile);
            PersonalEntryTestData.SeedRate(_rates, PersonalEntryTestData.NewerRate);

            Result result = await ExecuteAsync(changedMeanwhile.Id, PersonalEntryTestData.Input(_pets.Id, 120_000, "MKD"));

            Assert.True(result.IsSuccess);
            Assert.Equal(Currency.MKD, changedMeanwhile.Currency);
            Assert.Equal(PersonalEntryTestData.NewerRate.MkdPerEur, changedMeanwhile.MkdPerEur);
        }

        [Fact]
        public async Task Execute_KeepsADateOlderThanTheLast24Months_Succeeds()
        {
            PersonalEntry old = PersonalEntryTestData.OldSpending(_pets.Id, new DateOnly(2023, 5, 4));
            _entries.Seed(old);

            Result result = await ExecuteAsync(old.Id, PersonalEntryTestData.Input(_pets.Id, 300_000, entryDate: "2023-05-04"));

            Assert.True(result.IsSuccess);
            Assert.Equal(300_000, old.AmountMinor);
            Assert.Equal(new DateOnly(2023, 5, 4), old.EntryDate);
        }

        [Fact]
        public async Task Execute_MovesAnOldDateToAnotherDateOutsideTheLast24Months_FailsWithDateInvalidAndChangesNothing()
        {
            PersonalEntry old = PersonalEntryTestData.OldSpending(_pets.Id, new DateOnly(2023, 5, 4));
            _entries.Seed(old);

            Result result = await ExecuteAsync(old.Id, PersonalEntryTestData.Input(_pets.Id, 300_000, entryDate: "2023-05-05"));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
            Assert.Equal(PersonalEntryTestData.OneThousandMkd, old.AmountMinor);
            Assert.Null(old.UpdatedAt);
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
            Result result = await _editPersonalEntry.Execute(PersonalEntryTestData.OtherUserId, _entry.Id, PersonalEntryTestData.Input(_pets.Id, 300_000), TestContext.Current.CancellationToken);

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

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE);
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

        private void SeedStaleRate()
        {
            _rates.Seed(PersonalEntryTestData.Rate.RateDate, PersonalEntryTestData.Rate.MkdPerEur, Later.AddHours(-9));
        }

        private Task<Result> ExecuteAsync(Guid entryId, PersonalEntryInput input)
        {
            return _editPersonalEntry.Execute(PersonalEntryTestData.UserId, entryId, input, TestContext.Current.CancellationToken);
        }
    }
}
