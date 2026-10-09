using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class AddExpenseMembersTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Add_PayerThatIsNotAnyMember_Answers404MemberNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, Guid.CreateVersion7(), group.OwnerRowId, group.MemberRowId);

            await AssertRejectedAsync(group, input, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Add_PayerFromAnotherGroup_Answers404MemberNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseGroup other = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, other.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            await AssertRejectedAsync(group, input, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Add_PayerWhoWasRemovedFromTheGroup_Answers404MemberNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await ExpenseRows.EndMemberRowAsync(_app, group.MemberRowId, group.Owner.UserId);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId);

            await AssertRejectedAsync(group, input, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Add_PayerThatIsAPlainNameRemovedFromTheGroup_Answers404MemberNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.PlainRowIds[0], group.OwnerRowId);

            await AssertRejectedAsync(group, input, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Add_ShareMemberThatIsNotAnyMember_Answers404MemberNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, Guid.CreateVersion7());

            await AssertRejectedAsync(group, input, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Add_ShareMemberFromAnotherGroup_Answers404MemberNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseGroup other = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, other.MemberRowId);

            await AssertRejectedAsync(group, input, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Add_ShareMemberWhoWasRemovedFromTheGroup_Answers404MemberNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.PlainRowIds[0]);

            await AssertRejectedAsync(group, input, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Add_ByANonMember_Answers404GroupNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(stranger.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_ByAMemberRemovedFromTheGroup_Answers404GroupNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Removed");
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_InADeletedGroup_Answers404GroupNotFoundEvenForTheOwner()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(0, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_InAGroupThatDoesNotExist_Answers404GroupNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, Guid.CreateVersion7(), input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Add_AnExpenseWithAMemberWhoLaterLeaves_KeepsTheMemberInTheSplit()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            Assert.Equal(2, await ExpenseRows.CountSharesOfAsync(_app, expenseId));
            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);
            Assert.Equal([GroupsApp.OwnerName, "Marko"], ExpenseJson.ShareNames(detail));
        }

        private async Task AssertRejectedAsync(ExpenseGroup group, ExpenseInput input, string expectedErrorCode)
        {
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, expectedErrorCode);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }
    }
}
