using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class DuplicateAnswerAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(DuplicateAnswerRequests.AllEndpoints), MemberType = typeof(DuplicateAnswerRequests))]
        public async Task Anonymous_Answers401(string endpoint)
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await DuplicateAnswerRequests.SendAsync(anonymous, endpoint, Guid.CreateVersion7(), Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(DuplicateAnswerRequests.AllEndpoints), MemberType = typeof(DuplicateAnswerRequests))]
        public async Task Guest_Answers403GuestNotAllowedAndWritesNothing(string endpoint)
        {
            GuestMember guest = await JoinAsGuestAsync();
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);

            HttpResponseMessage response = await DuplicateAnswerRequests.SendAsync(guest.Client, endpoint, Guid.CreateVersion7(), Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Guest_PutWithABadAnswer_Answers403BeforeCheckingTheAnswer()
        {
            GuestMember guest = await JoinAsGuestAsync();
            string json = $"{{\"entryId\":\"{Guid.CreateVersion7()}\",\"expenseId\":\"{Guid.CreateVersion7()}\",\"answer\":\"Maybe\"}}";

            HttpResponseMessage response = await DuplicateAnswerRequests.PutWithJsonTextAsync(guest.Client, json);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        [Fact]
        public async Task Guest_ThatHasAShareInAnExpense_StillAnswers403AndStoresNothing()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            ExpenseInput input = ExpenseInputs.Equal(PossibleDuplicateSteps.WholeMinor, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.PlainRowIds[0]) with { CategoryId = PossibleDuplicateSteps.Food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(guest.Client, Guid.CreateVersion7(), expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, guest.UserId));
        }

        [Fact]
        public async Task Put_ByOnePerson_DoesNotChangeTheOtherPersonsPairsEntriesOrTotals()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid memberEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, memberEntry, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
            Assert.Equal(PossibleDuplicateSteps.Keys((ownerEntry, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Owner.Client));
            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(group.Owner.Client, ownerEntry));
            Assert.Equal(PossibleDuplicateSteps.ShareMinor, await PossibleDuplicateSteps.MonthSpendingTotalAsync(group.Owner.Client));
        }

        private async Task<GuestMember> JoinAsGuestAsync()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);

            return await GuestSteps.JoinWithNewNameAsync(_app, groupId, "Gost");
        }
    }
}
