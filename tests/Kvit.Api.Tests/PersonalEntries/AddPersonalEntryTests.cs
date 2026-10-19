using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.PersonalCategories;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class AddPersonalEntryTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Add_WithABuiltInCategory_Answers200WithTheSavedEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, 61.5000m, TimeSpan.FromHours(1));

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 125_000, "MKD", "2026-10-04", "  Lunch  "));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(PersonalEntryRequests.EntryFieldNames, body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
            Assert.Equal(PersonalEntryRequests.BuiltInFoodId, body.GetProperty("categoryId").GetGuid());
            Assert.Equal(125_000, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal("2026-10-04", body.GetProperty("entryDate").GetString());
            Assert.Equal("Lunch", body.GetProperty("note").GetString());
            Assert.Equal(61.5000m, body.GetProperty("mkdPerEur").GetDecimal());
            Assert.Equal("2026-10-05", body.GetProperty("rateDate").GetString());
            Assert.Equal(ExpensesApp.ClockStart, body.GetProperty("createdAt").GetDateTimeOffset());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("updatedAt").ValueKind);
        }

        [Fact]
        public async Task Add_WithABuiltInCategory_EmbedsTheCategoryWithItsKeyEmojiAndColor()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId)));

            JsonElement category = body.GetProperty("category");
            Assert.Equal(PersonalCategoryRequests.RowFieldNames, category.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.Equal(PersonalEntryRequests.BuiltInFoodId, category.GetProperty("id").GetGuid());
            Assert.Equal("food", category.GetProperty("key").GetString());
            Assert.Equal(JsonValueKind.Null, category.GetProperty("name").ValueKind);
            Assert.Equal("orange", category.GetProperty("color").GetString());
            Assert.False(category.GetProperty("isCustom").GetBoolean());
            Assert.False(category.GetProperty("archived").GetBoolean());
        }

        [Fact]
        public async Task Add_WithOwnCustomCategory_Answers200WithThatCategoryEmbedded()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(pets, 1_550, "EUR"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(pets, body.GetProperty("categoryId").GetGuid());
            Assert.Equal("Pets", body.GetProperty("category").GetProperty("name").GetString());
            Assert.True(body.GetProperty("category").GetProperty("isCustom").GetBoolean());
            Assert.Equal("EUR", body.GetProperty("currency").GetString());
        }

        [Fact]
        public async Task Add_ValidEntry_StoresOneSpendingRowOfTheCallerWithTheClientRequestId()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid clientRequestId = Guid.CreateVersion7();

            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId), clientRequestId);

            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
            Assert.Equal(user.UserId.ToString(), await PersonalEntrySteps.ValueAsync(_app, entryId, "user_id"));
            Assert.Equal("Spending", await PersonalEntrySteps.ValueAsync(_app, entryId, "kind"));
            Assert.Equal(clientRequestId.ToString(), await PersonalEntrySteps.ValueAsync(_app, entryId, "client_request_id"));
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at"));
        }

        [Fact]
        public async Task Add_ValidEntry_WritesNoActivityEventAndNoUsageEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            int before = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));

            Assert.Equal(before, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_WithAnArchivedOwnCategory_Answers400CategoryInvalidAndWritesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid archived = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Old");

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(archived));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_WithTheCategoryOfAnotherUser_Answers400CategoryInvalidAndWritesNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid pets = await PersonalCategorySteps.CreateAsync(owner.Client, "Pets");

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(other.Client, EntryInput.Of(pets));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, other.UserId));
        }

        [Fact]
        public async Task Add_WithAnUnknownCategory_Answers400CategoryInvalid()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(Guid.CreateVersion7()));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
        }

        [Fact]
        public async Task Add_WithoutACategoryId_Answers400CategoryInvalid()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(Guid.Empty));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
        }

        [Theory]
        [InlineData(0, "MKD", "2026-10-04", ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE)]
        [InlineData(-100, "MKD", "2026-10-04", ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE)]
        [InlineData(1_000_000_000_000, "EUR", "2026-10-04", ResultCodes.EXPENSE_AMOUNT_TOO_LARGE)]
        [InlineData(150, "MKD", "2026-10-04", ResultCodes.MONEY_NOT_ON_CURRENCY_STEP)]
        [InlineData(100_000, "USD", "2026-10-04", ResultCodes.EXPENSE_CURRENCY_INVALID)]
        [InlineData(100_000, "MKD", "1999-12-31", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        [InlineData(100_000, "MKD", "2027-10-07", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        [InlineData(100_000, "MKD", "04.10.2026", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        public async Task Add_InvalidField_Answers400WithItsCodeAndWritesNothing(long amountMinor, string currency, string entryDate, string expectedErrorCode)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, amountMinor, currency, entryDate));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_DateOneYearAndOneDayAhead_IsAcceptedLikeExpenses()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, entryDate: "2027-10-06"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Add_NoteOf501Characters_Answers400ExpenseNoteInvalid()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, note: new string('a', 501)));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_NOTE_INVALID);
        }

        [Fact]
        public async Task Add_WithoutAClientRequestId_Answers400AndWritesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId), Guid.Empty);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }
    }
}
