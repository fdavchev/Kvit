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
    public class AddIncomeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Add_Income_Answers200WithTheSavedEntryOfKindIncomeAndNoCategory()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, 61.5000m, TimeSpan.FromHours(1));

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(4_500_000, "MKD", "2026-10-04", "  Salary  "));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(PersonalEntryRequests.EntryFieldNames, body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
            Assert.Equal("Income", body.GetProperty("kind").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("categoryId").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("category").ValueKind);
            Assert.Equal(4_500_000, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal("2026-10-04", body.GetProperty("entryDate").GetString());
            Assert.Equal("Salary", body.GetProperty("note").GetString());
            Assert.Equal(61.5000m, body.GetProperty("mkdPerEur").GetDecimal());
            Assert.Equal("2026-10-05", body.GetProperty("rateDate").GetString());
            Assert.Equal(ExpensesApp.ClockStart, body.GetProperty("createdAt").GetDateTimeOffset());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("updatedAt").ValueKind);
        }

        [Fact]
        public async Task Add_IncomeInEuro_Answers200WithTheCents()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(123_456, "EUR"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("EUR", body.GetProperty("currency").GetString());
            Assert.Equal(123_456, body.GetProperty("amountMinor").GetInt64());
        }

        [Fact]
        public async Task Add_IncomeWithoutANote_AnswersANullNote()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await AuthRequests.ReadJsonAsync(await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(note: "  ")));

            Assert.Equal(JsonValueKind.Null, body.GetProperty("note").ValueKind);
        }

        [Fact]
        public async Task Add_Income_StoresOneIncomeRowOfTheCallerWithNoCategoryAndNoRecurringIncome()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid clientRequestId = Guid.CreateVersion7();

            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(), clientRequestId);

            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
            Assert.Equal(user.UserId.ToString(), await PersonalEntrySteps.ValueAsync(_app, entryId, "user_id"));
            Assert.Equal("Income", await PersonalEntrySteps.ValueAsync(_app, entryId, "kind"));
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "category_id"));
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "recurring_income_id"));
            Assert.Equal(clientRequestId.ToString(), await PersonalEntrySteps.ValueAsync(_app, entryId, "client_request_id"));
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at"));
        }

        [Fact]
        public async Task Add_Income_WritesNoActivityEventAndNoUsageEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            int before = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());

            Assert.Equal(before, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_IncomeWithABuiltInCategoryId_Answers400IncomeHasCategoryAndWritesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeWithCategoryAsync(user.Client, IncomeEntryInput.Of(), PersonalEntryRequests.BuiltInFoodId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, PersonalEntryRequests.IncomeHasCategoryCode);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_IncomeWithOwnCustomCategoryId_Answers400IncomeHasCategoryAndWritesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeWithCategoryAsync(user.Client, IncomeEntryInput.Of(), pets);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, PersonalEntryRequests.IncomeHasCategoryCode);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_IncomeWithAnUnknownCategoryId_Answers400IncomeHasCategory()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeWithCategoryAsync(user.Client, IncomeEntryInput.Of(), Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, PersonalEntryRequests.IncomeHasCategoryCode);
        }

        [Fact]
        public async Task Add_IncomeWithANullCategoryId_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddWithBodyAsync(
                user.Client,
                new { clientRequestId = Guid.CreateVersion7(), kind = "Income", categoryId = (Guid?)null, amountMinor = 4_500_000, currency = "MKD", entryDate = "2026-10-04", note = (string?)null });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData(0, "MKD", "2026-10-04", ResultCodes.PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE)]
        [InlineData(-100, "MKD", "2026-10-04", ResultCodes.PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE)]
        [InlineData(1_000_000_000_000, "EUR", "2026-10-04", ResultCodes.PERSONAL_ENTRY_AMOUNT_TOO_LARGE)]
        [InlineData(150, "MKD", "2026-10-04", ResultCodes.MONEY_NOT_ON_CURRENCY_STEP)]
        [InlineData(100_000, "USD", "2026-10-04", ResultCodes.PERSONAL_ENTRY_CURRENCY_INVALID)]
        [InlineData(100_000, "MKD", "1999-12-31", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        [InlineData(100_000, "MKD", "2024-10-30", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        [InlineData(100_000, "MKD", "2026-10-07", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        [InlineData(100_000, "MKD", "2027-10-06", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        [InlineData(100_000, "MKD", "04.10.2026", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        public async Task Add_IncomeWithAnInvalidField_Answers400WithItsCodeAndWritesNothing(long amountMinor, string currency, string entryDate, string expectedErrorCode)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(amountMinor, currency, entryDate));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Theory]
        [InlineData("2024-10-31")]
        [InlineData("2024-11-01")]
        [InlineData("2026-10-05")]
        [InlineData("2026-10-06")]
        public async Task Add_IncomeDateOnTheEdgesOfTheLast24MonthsUpToTomorrowInUtc_IsAccepted(string entryDate)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(entryDate: entryDate));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(entryDate, (await AuthRequests.ReadJsonAsync(response)).GetProperty("entryDate").GetString());
        }

        [Fact]
        public async Task Add_IncomeNoteOf500Characters_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(note: new string('a', 500)));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Add_IncomeNoteOf501Characters_Answers400PersonalEntryNoteInvalid()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(note: new string('a', 501)));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_NOTE_INVALID);
        }

        [Fact]
        public async Task Add_IncomeWithoutAClientRequestId_Answers400AndWritesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(), Guid.Empty);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_WithoutAKindButWithACategory_SavesASpendingEntryLikeOldClients()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddWithBodyAsync(
                user.Client,
                new { clientRequestId = Guid.CreateVersion7(), categoryId = PersonalEntryRequests.BuiltInFoodId, amountMinor = 100_000, currency = "MKD", entryDate = "2026-10-04" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Spending", body.GetProperty("kind").GetString());
            Assert.Equal(PersonalEntryRequests.BuiltInFoodId, body.GetProperty("categoryId").GetGuid());
        }

        [Fact]
        public async Task Add_WithoutAKindAndWithoutACategory_Answers400CategoryInvalidLikeBefore()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddWithBodyAsync(
                user.Client,
                new { clientRequestId = Guid.CreateVersion7(), amountMinor = 100_000, currency = "MKD", entryDate = "2026-10-04" });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_KindSpendingWithACategory_SavesASpendingEntry()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddWithBodyAsync(
                user.Client,
                new { clientRequestId = Guid.CreateVersion7(), kind = "Spending", categoryId = PersonalEntryRequests.BuiltInFoodId, amountMinor = 100_000, currency = "MKD", entryDate = "2026-10-04" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Spending", (await AuthRequests.ReadJsonAsync(response)).GetProperty("kind").GetString());
        }

        [Fact]
        public async Task Add_KindSpendingWithoutACategory_Answers400CategoryInvalid()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddWithBodyAsync(
                user.Client,
                new { clientRequestId = Guid.CreateVersion7(), kind = "Spending", amountMinor = 100_000, currency = "MKD", entryDate = "2026-10-04" });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_AnUnknownKindEvenWithACategory_Answers400AndWritesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.AddWithBodyAsync(
                user.Client,
                new { clientRequestId = Guid.CreateVersion7(), kind = "Savings", categoryId = PersonalEntryRequests.BuiltInFoodId, amountMinor = 100_000, currency = "MKD", entryDate = "2026-10-04" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }
    }
}
