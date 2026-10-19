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
    public class EditPersonalEntryTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Edit_OwnEntry_Answers200WithTheUpdatedEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(user.Client, entryId, EntryInput.Of(PersonalEntryRequests.BuiltInTransportId, 250_000, "MKD", "2026-10-03", " Taxi "));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(PersonalEntryRequests.EntryFieldNames, body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.Equal(entryId, body.GetProperty("id").GetGuid());
            Assert.Equal(PersonalEntryRequests.BuiltInTransportId, body.GetProperty("categoryId").GetGuid());
            Assert.Equal("transport", body.GetProperty("category").GetProperty("key").GetString());
            Assert.Equal(250_000, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("2026-10-03", body.GetProperty("entryDate").GetString());
            Assert.Equal("Taxi", body.GetProperty("note").GetString());
            Assert.Equal(_app.Clock.GetUtcNow(), body.GetProperty("updatedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Edit_OwnEntry_StoresTheNewValuesAndWritesNoEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, note: "Lunch"));
            int eventsBefore = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await PersonalEntryRequests.EditAsync(user.Client, entryId, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 300_000, note: ""));

            Assert.Equal("300000", await PersonalEntrySteps.ValueAsync(_app, entryId, "amount_minor"));
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "note"));
            Assert.Equal(eventsBefore, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Edit_KeepingItsCategoryThatWasArchivedSince_Answers200AndKeepsIt()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(pets));
            await PersonalCategoryRequests.ArchiveAsync(user.Client, pets);

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(user.Client, entryId, EntryInput.Of(pets, 300_000));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(pets, body.GetProperty("categoryId").GetGuid());
            Assert.True(body.GetProperty("category").GetProperty("archived").GetBoolean());
            Assert.Equal(300_000, body.GetProperty("amountMinor").GetInt64());
        }

        [Fact]
        public async Task Edit_SwitchingToAnArchivedCategory_Answers400CategoryInvalidAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid archived = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Old");
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(user.Client, entryId, EntryInput.Of(archived));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_SwitchingToTheCategoryOfAnotherUser_Answers400CategoryInvalidAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid othersCategory = await PersonalCategorySteps.CreateAsync(other.Client, "Pets");
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(user.Client, entryId, EntryInput.Of(othersCategory));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_InvalidDate_Answers400PersonalEntryDateInvalidAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(user.Client, entryId, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, entryDate: "1999-12-31"));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_DeletedEntry_Answers404NotFound()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(user.Client, entryId, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 300_000));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        [Fact]
        public async Task Edit_UnknownEntry_Answers404NotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(user.Client, Guid.CreateVersion7(), EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }
    }
}
