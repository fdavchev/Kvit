using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Guards;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class AddExpenseLateRepeatTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Add_RepeatAfterTheGroupBecameClosing_Answers200WithTheSameExpenseAndLeavesTheClosingAlone()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            ExpenseInput input = GuardSteps.MemberExpenseInput(group);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input, clientRequestId);
            await GuardStates.ForceClosingAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expenseId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid());
            Assert.Equal("Closing", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_RepeatAfterTheGroupFinished_Answers200WithTheSameExpenseAndLeavesTheGroupFinished()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            ExpenseInput input = GuardSteps.MemberExpenseInput(group);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input, clientRequestId);
            await GuardStates.ForceFinishedAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expenseId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid());
            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_RepeatAfterTheGroupWasDeleted_Answers404GroupNotFoundEvenForTheOwner()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            ExpenseInput input = GuardSteps.OwnerExpenseInput(group);
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input, clientRequestId);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        public async Task Add_RepeatAfterTheCallerNoLongerBelongsToTheGroup_Answers404GroupNotFound(string endKind)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            ExpenseInput input = GuardSteps.MemberExpenseInput(group);
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.AddExpenseAsync(group.Member, group.GroupId, input, clientRequestId);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_RepeatAfterTheExpenseWasDeleted_Answers404ExpenseNotFoundAndSavesNothingNew()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            ExpenseInput input = GuardSteps.MemberExpenseInput(group);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input, clientRequestId);
            await GuardSteps.AssertNoContentAsync(ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId));
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_RepeatAfterTheExpenseWasEdited_Answers200WithTheCurrentStateNotTheFirstAnswer()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            ExpenseInput input = GuardSteps.MemberExpenseInput(group) with { Title = "Dinner" };
            ExpenseInput edited = input with { Title = "Dinner and dessert", AmountMinor = GuardSteps.BiggerExpenseMinor };
            Guid clientRequestId = Guid.CreateVersion7();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input, clientRequestId);
            await GuardSteps.AssertNoContentAsync(ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, edited));

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(expenseId, body.GetProperty("id").GetGuid());
            Assert.Equal("Dinner and dessert", body.GetProperty("title").GetString());
            Assert.Equal(GuardSteps.BiggerExpenseMinor, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_RepeatWithASignedOutCookie_Answers401AndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            ExpenseInput input = GuardSteps.MemberExpenseInput(group);
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.AddExpenseAsync(group.Member, group.GroupId, input, clientRequestId);
            HttpResponseMessage loggedOut = await AuthRequests.LogOutAsync(group.Member.Client);
            Assert.Equal(HttpStatusCode.NoContent, loggedOut.StatusCode);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input, clientRequestId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, group.Member.UserId, "ExpenseAdded"));
        }
    }
}
