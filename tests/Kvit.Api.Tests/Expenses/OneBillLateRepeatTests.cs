using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Guards;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class OneBillLateRepeatTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task OneBill_RepeatAfterTheBillGroupFinished_Answers200WithTheSameGroupIdAndExpenseId()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();
            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input, clientRequestId);
            await GuardStates.ForceFinishedAsync(_app, groupId);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(groupId, body.GetProperty("groupId").GetGuid());
            Assert.Equal(expenseId, body.GetProperty("expenseId").GetGuid());
            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, groupId, "status"));
            Assert.Equal(1, await GroupRows.CountGroupsOwnedByAsync(_app, caller.UserId));
        }

        [Fact]
        public async Task OneBill_RepeatAfterTheBillGroupWasDeleted_Answers200WithTheSameGroupIdAndExpenseId()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();
            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input, clientRequestId);
            await GroupRows.SetDeletedAsync(_app, groupId, caller.UserId, 0);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(groupId, body.GetProperty("groupId").GetGuid());
            Assert.Equal(expenseId, body.GetProperty("expenseId").GetGuid());
            Assert.NotNull(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
            Assert.Equal(1, await GroupRows.CountGroupsOwnedByAsync(_app, caller.UserId));
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        public async Task OneBill_RepeatAfterTheCallerNoLongerBelongsToTheBillGroup_Answers400ClientRequestIdUsedAndRevealsNeitherId(string endKind)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();
            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input, clientRequestId);
            await GroupRows.EndMembershipAsync(_app, groupId, caller.UserId, endKind);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input, clientRequestId);

            string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(ResultCodes.EXPENSE_CLIENT_REQUEST_ID_USED, text);
            Assert.DoesNotContain(groupId.ToString(), text);
            Assert.DoesNotContain(expenseId.ToString(), text);
            Assert.Equal(1, await GroupRows.CountGroupsOwnedByAsync(_app, caller.UserId));
        }

        [Fact]
        public async Task OneBill_RepeatByAnotherPersonWithTheSameBody_Answers400ClientRequestIdUsedAndRevealsNeitherIdOfTheBill()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();
            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(first, input, clientRequestId);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(second.Client, input, clientRequestId);

            string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(ResultCodes.EXPENSE_CLIENT_REQUEST_ID_USED, text);
            Assert.DoesNotContain(groupId.ToString(), text);
            Assert.DoesNotContain(expenseId.ToString(), text);
            await _app.AssertNothingWrittenForAsync(second);
        }
    }
}
