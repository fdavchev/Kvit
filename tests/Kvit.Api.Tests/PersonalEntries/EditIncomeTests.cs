using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class EditIncomeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Edit_OwnIncome_Answers200WithTheUpdatedIncomeEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of(5_000_000, "MKD", "2026-10-03", " Raise "));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(PersonalEntryRequests.EntryFieldNames, body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.Equal(entryId, body.GetProperty("id").GetGuid());
            Assert.Equal("Income", body.GetProperty("kind").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("categoryId").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("category").ValueKind);
            Assert.Equal(5_000_000, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("2026-10-03", body.GetProperty("entryDate").GetString());
            Assert.Equal("Raise", body.GetProperty("note").GetString());
            Assert.Equal(_app.Clock.GetUtcNow(), body.GetProperty("updatedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Edit_OwnIncome_StoresTheNewValuesKeepsTheKindAndWritesNoEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(note: "Salary"));
            int eventsBefore = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of(3_000_000, note: ""));

            Assert.Equal("3000000", await PersonalEntrySteps.ValueAsync(_app, entryId, "amount_minor"));
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "note"));
            Assert.Equal("Income", await PersonalEntrySteps.ValueAsync(_app, entryId, "kind"));
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "category_id"));
            Assert.Equal(eventsBefore, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Edit_IncomeWithACategoryId_Answers400IncomeHasCategoryAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditWithBodyAsync(
                user.Client,
                entryId,
                new { kind = "Income", categoryId = PersonalEntryRequests.BuiltInFoodId, amountMinor = 3_000_000, currency = "MKD", entryDate = "2026-10-04" });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, PersonalEntryRequests.IncomeHasCategoryCode);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Theory]
        [InlineData(0, "MKD", "2026-10-04", ResultCodes.PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE)]
        [InlineData(1_000_000_000_000, "EUR", "2026-10-04", ResultCodes.PERSONAL_ENTRY_AMOUNT_TOO_LARGE)]
        [InlineData(150, "MKD", "2026-10-04", ResultCodes.MONEY_NOT_ON_CURRENCY_STEP)]
        [InlineData(100_000, "USD", "2026-10-04", ResultCodes.PERSONAL_ENTRY_CURRENCY_INVALID)]
        [InlineData(100_000, "MKD", "1999-12-31", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        [InlineData(100_000, "MKD", "2024-10-30", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        [InlineData(100_000, "MKD", "2026-10-07", ResultCodes.PERSONAL_ENTRY_DATE_INVALID)]
        public async Task Edit_IncomeWithAnInvalidField_Answers400WithItsCodeAndChangesNothing(long amountMinor, string currency, string entryDate, string expectedErrorCode)
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of(amountMinor, currency, entryDate));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_IncomeNoteOf501Characters_Answers400PersonalEntryNoteInvalid()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of(note: new string('a', 501)));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_NOTE_INVALID);
        }

        [Theory]
        [InlineData("2024-10-31")]
        [InlineData("2026-10-06")]
        public async Task Edit_IncomeDateMovedToTheEdgesOfTheAllowedRange_Answers200(string entryDate)
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of(entryDate: entryDate));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Edit_KeepingAnIncomeDateThatTimeHasMovedOutOfTheLast24Months_Answers200WithTheNewAmount()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());
            await PersonalEntrySteps.SetEntryDateAsync(_app, entryId, new DateOnly(2023, 5, 4));

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of(3_000_000, entryDate: "2023-05-04"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("2023-05-04", body.GetProperty("entryDate").GetString());
            Assert.Equal(3_000_000, body.GetProperty("amountMinor").GetInt64());
        }

        [Fact]
        public async Task Edit_MovingAnOldIncomeDateToAnotherOldDate_Answers400PersonalEntryDateInvalidAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());
            await PersonalEntrySteps.SetEntryDateAsync(_app, entryId, new DateOnly(2023, 5, 4));
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of(3_000_000, entryDate: "2023-05-05"));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_DeletedIncome_Answers404NotFound()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAndDeleteAsync(user.Client, IncomeEntryInput.Of());

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of(3_000_000));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        [Fact]
        public async Task Edit_UnknownEntryAsIncome_Answers404NotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, Guid.CreateVersion7(), IncomeEntryInput.Of());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        [Fact]
        public async Task Edit_AnIncomeEntryAsSpending_Answers400KindCannotChangeAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditWithBodyAsync(
                user.Client,
                entryId,
                new { kind = "Spending", categoryId = PersonalEntryRequests.BuiltInFoodId, amountMinor = 100_000, currency = "MKD", entryDate = "2026-10-04" });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, PersonalEntryRequests.KindCannotChangeCode);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_ASpendingEntryAsIncome_Answers400KindCannotChangeAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, PersonalEntryRequests.KindCannotChangeCode);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_ASpendingEntryWithKindSpending_Answers200AndKeepsItsCategory()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));

            HttpResponseMessage response = await PersonalEntryRequests.EditWithBodyAsync(
                user.Client,
                entryId,
                new { kind = "Spending", categoryId = PersonalEntryRequests.BuiltInTransportId, amountMinor = 250_000, currency = "MKD", entryDate = "2026-10-03" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Spending", body.GetProperty("kind").GetString());
            Assert.Equal(PersonalEntryRequests.BuiltInTransportId, body.GetProperty("categoryId").GetGuid());
        }

        [Fact]
        public async Task Edit_WithAnUnknownKindEvenWithACategory_Answers400AndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditWithBodyAsync(
                user.Client,
                entryId,
                new { kind = "Savings", categoryId = PersonalEntryRequests.BuiltInFoodId, amountMinor = 100_000, currency = "MKD", entryDate = "2026-10-04" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }
    }
}
