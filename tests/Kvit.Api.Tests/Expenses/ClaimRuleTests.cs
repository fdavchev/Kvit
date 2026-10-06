using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class ClaimRuleTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task CanClaimNames_MemberWithNothingRecordedUnderTheirName_IsTrue()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");

            Assert.True(await CanClaimNamesAsync(group.Member, group.GroupId));
        }

        [Fact]
        public async Task CanClaimNames_MemberWhoIsThePayerOfAnExpense_IsFalse()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.MemberRowId, group.OwnerRowId));

            Assert.False(await CanClaimNamesAsync(group.Member, group.GroupId));
        }

        [Fact]
        public async Task CanClaimNames_MemberWhoHoldsAShareOfAnExpense_IsFalse()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId));

            Assert.False(await CanClaimNamesAsync(group.Member, group.GroupId));
        }

        [Fact]
        public async Task CanClaimNames_MemberWhoHoldsAShareOfZero_IsFalse()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, ExpenseInputs.OneThousandMkd), new ShareInput(group.MemberRowId, 0)];
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, shares));

            Assert.False(await CanClaimNamesAsync(group.Member, group.GroupId));
        }

        [Fact]
        public async Task CanClaimNames_MemberWhoseOnlyExpenseWasDeleted_IsStillFalse()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.False(await CanClaimNamesAsync(group.Member, group.GroupId));
        }

        [Fact]
        public async Task CanClaimNames_MemberWhoIsNotInAnyExpenseWhileOthersAre_IsStillTrue()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.PlainRowIds[0]));

            Assert.True(await CanClaimNamesAsync(group.Member, group.GroupId));
            Assert.False(await CanClaimNamesAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task Claim_MemberWhoIsThePayerOfAnExpense_Answers400MemberCannotClaimAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.MemberRowId, group.OwnerRowId));
            string before = await GroupRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.PlainRowIds[0]);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Claim_MemberWhoHoldsAShareOfAnExpense_Answers400MemberCannotClaim()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId));

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.PlainRowIds[0]);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
        }

        [Fact]
        public async Task Claim_MemberWhoseOnlyExpenseWasDeleted_Answers400MemberCannotClaim()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.PlainRowIds[0]);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
        }

        [Fact]
        public async Task Claim_MemberWithNothingRecordedWhileTheNameHasExpenses_Answers204()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.PlainRowIds[0], group.OwnerRowId, group.PlainRowIds[0]));

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.PlainRowIds[0]);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private async Task<bool> CanClaimNamesAsync(SignedInUser viewer, Guid groupId)
        {
            JsonElement body = await GroupRequests.ReadMembersAsync(viewer.Client, groupId);

            return body.GetProperty("canClaimNames").GetBoolean();
        }
    }
}
