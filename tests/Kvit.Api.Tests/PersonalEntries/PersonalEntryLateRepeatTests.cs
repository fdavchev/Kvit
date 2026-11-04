using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class PersonalEntryLateRepeatTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Add_SameClientRequestIdSentAsIncomeAfterItSavedAsSpending_AnswersTheFirstSpendingEntryUnchanged()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, note: "Lunch"), clientRequestId);
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(note: "Salary"), clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(entryId, body.GetProperty("id").GetGuid());
            Assert.Equal(PersonalEntryRequests.SpendingKind, body.GetProperty("kind").GetString());
            Assert.Equal(PersonalEntryRequests.BuiltInFoodId, body.GetProperty("categoryId").GetGuid());
            Assert.Equal(PersonalEntryRequests.OneThousandMkd, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("Lunch", body.GetProperty("note").GetString());
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Add_SameClientRequestIdSentAsSpendingAfterItSavedAsIncome_AnswersTheFirstIncomeEntryUnchanged()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(note: "Salary"), clientRequestId);
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, note: "Lunch"), clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(entryId, body.GetProperty("id").GetGuid());
            Assert.Equal(PersonalEntryRequests.IncomeKind, body.GetProperty("kind").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("categoryId").ValueKind);
            Assert.Equal(IncomeEntryInput.Salary, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("Salary", body.GetProperty("note").GetString());
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Add_RepeatByAGuestOfASavedSpendingRequestId_Answers403GuestNotAllowedAndRevealsNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid entryId = await PersonalEntrySteps.AddAsync(owner.Client, input, clientRequestId);
            GuestMember guest = await JoinAsGuestAsync();
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(guest.Client, input, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.DoesNotContain(entryId.ToString(), await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Add_RepeatByAGuestOfASavedIncomeRequestId_Answers403GuestNotAllowedAndRevealsNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            IncomeEntryInput input = IncomeEntryInput.Of();
            Guid clientRequestId = Guid.CreateVersion7();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(owner.Client, input, clientRequestId);
            GuestMember guest = await JoinAsGuestAsync();
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(guest.Client, input, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.DoesNotContain(entryId.ToString(), await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        private async Task<GuestMember> JoinAsGuestAsync()
        {
            SignedInUser groupOwner = await _app.SignUpAsync("Zoran");
            Guid groupId = await _app.CreateGroupAsync(groupOwner);

            return await GuestSteps.JoinWithNewNameAsync(_app, groupId, "Gost");
        }
    }
}
