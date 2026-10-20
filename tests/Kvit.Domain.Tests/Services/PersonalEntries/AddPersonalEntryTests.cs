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
    public class AddPersonalEntryTests
    {
        private readonly FakePersonalEntryRepository _entries = new();
        private readonly FakeCategoryRepository _categories = new();
        private readonly FakeExchangeRateRepository _rates = new();
        private readonly FakeExchangeRateSource _rateSource = new();
        private readonly AddPersonalEntry _addPersonalEntry;
        private readonly Category _pets = CategoryTestData.NewCustom("Pets", 11);

        public AddPersonalEntryTests()
        {
            _categories.Seed(_pets);
            PersonalEntryTestData.SeedRate(_rates, PersonalEntryTestData.Rate);
            FixedTimeProvider timeProvider = new(PersonalEntryTestData.Moment);
            _addPersonalEntry = new AddPersonalEntry(_entries, _categories, PersonalEntryTestData.CurrentRateFrom(_rates, _rateSource, timeProvider), timeProvider);
        }

        [Fact]
        public async Task Execute_ValidInputWithAnOwnCategory_AddsOneSpendingEntryOfTheCallerAndReturnsItsId()
        {
            Guid clientRequestId = Guid.CreateVersion7();

            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.UserId, clientRequestId, PersonalEntryTestData.Input(_pets.Id, 1_550, "EUR", "2026-10-18", " Vet "));

            Assert.True(result.IsSuccess);
            PersonalEntry added = Assert.Single(_entries.Added);
            Assert.Equal(added.Id, result.Value);
            Assert.Equal(PersonalEntryTestData.UserId, added.UserId);
            Assert.Equal(PersonalEntryKind.Spending, added.Kind);
            Assert.Equal(_pets.Id, added.CategoryId);
            Assert.Equal(1_550, added.AmountMinor);
            Assert.Equal(Currency.EUR, added.Currency);
            Assert.Equal(new DateOnly(2026, 10, 18), added.EntryDate);
            Assert.Equal("Vet", added.Note);
            Assert.Equal(clientRequestId, added.ClientRequestId);
            Assert.Equal(PersonalEntryTestData.Moment, added.CreatedAt);
        }

        [Fact]
        public async Task Execute_ValidInput_SavesTheCurrentRate()
        {
            await ExecuteAsync(PersonalEntryTestData.UserId, Guid.CreateVersion7(), PersonalEntryTestData.Input(_pets.Id));

            PersonalEntry added = Assert.Single(_entries.Added);
            Assert.Equal(PersonalEntryTestData.Rate.MkdPerEur, added.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.Rate.RateDate, added.RateDate);
        }

        [Fact]
        public async Task Execute_ValidInputWhileTheNewestRateIsStale_AsksTheSourceAndSavesItsRate()
        {
            SeedStaleRate();
            _rateSource.AnswerWith(PersonalEntryTestData.UtcToday, 61.9000m);

            await ExecuteAsync(PersonalEntryTestData.UserId, Guid.CreateVersion7(), PersonalEntryTestData.Input(_pets.Id));

            PersonalEntry added = Assert.Single(_entries.Added);
            Assert.Equal([PersonalEntryTestData.UtcToday], _rateSource.RequestedDates);
            Assert.Equal(61.9000m, added.MkdPerEur);
        }

        [Fact]
        public async Task Execute_ClientRequestIdAlreadySavedByTheCallerWhileTheNewestRateIsStale_ReturnsThatEntryWithoutAskingTheSource()
        {
            PersonalEntry earlier = PersonalEntryTestData.NewSpending(_pets.Id);
            _entries.Seed(earlier);
            SeedStaleRate();
            _rateSource.FailWith("NBRM is down");

            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.UserId, earlier.ClientRequestId!.Value, PersonalEntryTestData.Input(_pets.Id));

            Assert.True(result.IsSuccess);
            Assert.Equal(earlier.Id, result.Value);
            Assert.Empty(_rateSource.RequestedDates);
        }

        [Fact]
        public async Task Execute_InvalidFieldWhileTheNewestRateIsStale_FailsWithoutAskingTheSource()
        {
            SeedStaleRate();

            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.UserId, Guid.CreateVersion7(), PersonalEntryTestData.Input(_pets.Id, 0));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE);
            Assert.Empty(_rateSource.RequestedDates);
        }

        [Fact]
        public async Task Execute_ClientRequestIdOfTheCallersDeletedEntry_FailsWithClientRequestIdUsedAndAddsNothing()
        {
            PersonalEntry earlier = PersonalEntryTestData.NewSpending(_pets.Id);
            earlier.Delete(PersonalEntryTestData.Moment);
            _entries.Seed(earlier);

            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.UserId, earlier.ClientRequestId!.Value, PersonalEntryTestData.Input(_pets.Id));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CLIENT_REQUEST_ID_USED);
            Assert.Empty(_entries.Added);
        }

        [Fact]
        public async Task Execute_ClientRequestIdAlreadySavedByTheCaller_ReturnsThatEntryAndAddsNothing()
        {
            PersonalEntry earlier = PersonalEntryTestData.NewSpending(_pets.Id);
            _entries.Seed(earlier);

            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.UserId, earlier.ClientRequestId!.Value, PersonalEntryTestData.Input(_pets.Id, 999_900));

            Assert.True(result.IsSuccess);
            Assert.Equal(earlier.Id, result.Value);
            Assert.Empty(_entries.Added);
        }

        [Fact]
        public async Task Execute_ClientRequestIdAlreadySavedByTheCallerAndTheInputIsNowInvalid_StillReturnsThatEntry()
        {
            PersonalEntry earlier = PersonalEntryTestData.NewSpending(_pets.Id);
            _entries.Seed(earlier);

            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.UserId, earlier.ClientRequestId!.Value, PersonalEntryTestData.Input(Guid.CreateVersion7(), 0, "USD"));

            Assert.True(result.IsSuccess);
            Assert.Equal(earlier.Id, result.Value);
        }

        [Fact]
        public async Task Execute_ClientRequestIdAlreadySavedByAnotherUser_FailsWithClientRequestIdUsedAndAddsNothing()
        {
            PersonalEntry earlier = PersonalEntryTestData.NewSpending(_pets.Id, PersonalEntryTestData.OtherUserId);
            _entries.Seed(earlier);

            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.UserId, earlier.ClientRequestId!.Value, PersonalEntryTestData.Input(_pets.Id));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CLIENT_REQUEST_ID_USED);
            Assert.Empty(_entries.Added);
        }

        [Fact]
        public async Task Execute_ArchivedOwnCategory_FailsWithCategoryInvalidAndAddsNothing()
        {
            Category archived = CategoryTestData.NewArchived("Old", 12, CategoryTestData.Moment);
            _categories.Seed(archived);

            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.UserId, Guid.CreateVersion7(), PersonalEntryTestData.Input(archived.Id));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            Assert.Empty(_entries.Added);
        }

        [Fact]
        public async Task Execute_CategoryOfAnotherUser_FailsWithCategoryInvalidAndAddsNothing()
        {
            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.OtherUserId, Guid.CreateVersion7(), PersonalEntryTestData.Input(_pets.Id));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            Assert.Empty(_entries.Added);
        }

        [Fact]
        public async Task Execute_UnknownCategory_FailsWithCategoryInvalid()
        {
            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.UserId, Guid.CreateVersion7(), PersonalEntryTestData.Input(Guid.CreateVersion7()));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
        }

        [Fact]
        public async Task Execute_InvalidFieldAndUnknownCategory_ReportsTheFieldFirst()
        {
            Result<Guid> result = await ExecuteAsync(PersonalEntryTestData.UserId, Guid.CreateVersion7(), PersonalEntryTestData.Input(Guid.CreateVersion7(), entryDate: "1999-12-31"));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
            Assert.Empty(_entries.Added);
        }

        private void SeedStaleRate()
        {
            _rates.Seed(PersonalEntryTestData.Rate.RateDate, PersonalEntryTestData.Rate.MkdPerEur, PersonalEntryTestData.Moment.AddHours(-9));
        }

        private Task<Result<Guid>> ExecuteAsync(Guid userId, Guid clientRequestId, PersonalEntryInput input)
        {
            return _addPersonalEntry.Execute(userId, clientRequestId, input, TestContext.Current.CancellationToken);
        }
    }
}
