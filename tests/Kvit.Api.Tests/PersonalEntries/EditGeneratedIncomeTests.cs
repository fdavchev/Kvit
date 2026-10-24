using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.RecurringIncomes;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class EditGeneratedIncomeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string PayDay = "2026-10-15";
        private const string RecurringIncomeIdProperty = "recurringIncomeId";

        [Fact]
        public async Task Edit_GeneratedEntryMovedToAnotherDate_Answers400DateLockedAndChangesNothing()
        {
            (SignedInUser user, _, Guid entryId) = await SalaryEntryAsync();
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of(5_000_000, entryDate: "2026-10-16"));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_LOCKED);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Edit_GeneratedEntryKeepingItsDate_Answers200WithTheNewAmountCurrencyNoteAndItsRecurringIncomeId()
        {
            (SignedInUser user, Guid recurringIncomeId, Guid entryId) = await SalaryEntryAsync();

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, IncomeEntryInput.Of(1_550, "EUR", PayDay, "Bonus"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(PayDay, body.GetProperty("entryDate").GetString());
            Assert.Equal(1_550, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", body.GetProperty("currency").GetString());
            Assert.Equal("Bonus", body.GetProperty("note").GetString());
            Assert.Equal(recurringIncomeId, body.GetProperty(RecurringIncomeIdProperty).GetGuid());
        }

        [Fact]
        public async Task DeleteAndRestore_GeneratedEntry_BothAnswer204()
        {
            (SignedInUser user, _, Guid entryId) = await SalaryEntryAsync();

            HttpResponseMessage deleted = await PersonalEntryRequests.DeleteAsync(user.Client, entryId);
            HttpResponseMessage restored = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at"));
        }

        [Fact]
        public async Task List_GeneratedEntryAndAnEntryTheUserAdded_CarryTheRecurringIncomeIdOrNull()
        {
            (SignedInUser user, Guid recurringIncomeId, Guid generatedId) = await SalaryEntryAsync();
            Guid addedId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(entryDate: "2026-10-18"));

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            Dictionary<Guid, JsonElement> entriesById = body.GetProperty("entries").EnumerateArray().ToDictionary(entry => entry.GetProperty("id").GetGuid());
            Assert.Equal(recurringIncomeId, entriesById[generatedId].GetProperty(RecurringIncomeIdProperty).GetGuid());
            Assert.Equal(JsonValueKind.Null, entriesById[addedId].GetProperty(RecurringIncomeIdProperty).ValueKind);
        }

        [Fact]
        public async Task Add_IncomeTheUserAdded_AnswersWithANullRecurringIncomeId()
        {
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(JsonValueKind.Null, body.GetProperty(RecurringIncomeIdProperty).ValueKind);
        }

        private async Task<(SignedInUser User, Guid RecurringIncomeId, Guid EntryId)> SalaryEntryAsync()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            Assert.Equal(1, await CatchUpSteps.CatchUpAsync(user.Client));
            Guid entryId = Guid.Parse((await CatchUpSteps.EntryValueAsync(_app, recurringIncomeId, "id"))!);

            return (user, recurringIncomeId, entryId);
        }
    }
}
