using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class RestoreExpenseTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        [Fact]
        public async Task Restore_ByWhoeverAddedIt_Answers204WithNoBody()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByMemberAsync();

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Restore_ByTheOwnerOfAnExpenseAddedByAnotherMember_Answers204()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByMemberAsync();

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Restore_ByAMemberWhoDidNotAddItAndIsNotTheOwner_Answers403ExpenseNotAllowedAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.EXPENSE_NOT_ALLOWED);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Restore_Expense_ClearsDeletedAtAndDeletedBy()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByMemberAsync();

            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "deleted_at"));
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "deleted_by_user_id"));
        }

        [Fact]
        public async Task Restore_Expense_BringsItBackInTheListAndTheDetailWithItsShares()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByMemberAsync();

            await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);

            JsonElement list = await AuthRequests.ReadJsonAsync(await ExpenseRequests.ListAsync(group.Owner.Client, group.GroupId));
            Assert.Equal([expenseId], ExpenseJson.IdsOf(list.GetProperty("expenses")));
            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);
            Assert.Equal(2, detail.GetProperty("shares").GetArrayLength());
        }

        [Fact]
        public async Task Restore_Expense_WritesOneExpenseRestoredEventByTheRestorerAboutTheExpense()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByMemberAsync();

            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseRestored"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseRestored", "actor_user_id"));
            Assert.Equal(expenseId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseRestored", "expense_id"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, group.Member.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Restore_ExpenseThatIsNotDeleted_Answers400ExpenseNotDeletedAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_NOT_DELETED);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Restore_NotDeletedExpenseByAMemberWhoMayNotRestore_Answers403BeforeTheStateCheck()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.EXPENSE_NOT_ALLOWED);
        }

        [Fact]
        public async Task Restore_ExpenseThatDoesNotExist_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Restore_DeletedExpenseOfAnotherGroupOfTheSameCaller_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(group.Owner);
            Guid ownerRowInOther = await GroupRows.CurrentMemberIdAsync(_app, otherGroupId, group.Owner.UserId);
            Guid expenseInOther = await _app.AddExpenseAsync(group.Owner, otherGroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", ownerRowInOther, ownerRowInOther));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, otherGroupId, expenseInOther);

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseInOther);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
            Assert.NotNull(await ExpenseRows.ExpenseValueAsync(_app, expenseInOther, "deleted_at"));
        }

        [Fact]
        public async Task Restore_OneSecondBeforeFiveDaysSinceTheDelete_Answers204()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByMemberAsync();
            _app.Clock.Advance(FiveDays - OneSecond);

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "deleted_at"));
        }

        [Fact]
        public async Task Restore_ExactlyFiveDaysSinceTheDelete_Answers400RestoreExpiredAndWritesNothing()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByMemberAsync();
            _app.Clock.Advance(FiveDays);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_RESTORE_EXPIRED);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Restore_OneSecondAfterFiveDaysSinceTheDelete_Answers400RestoreExpired()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByMemberAsync();
            _app.Clock.Advance(FiveDays + OneSecond);

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_RESTORE_EXPIRED);
        }

        [Fact]
        public async Task Restore_ExpiredExpenseByAMemberWhoMayNotRestore_Answers403BeforeTheExpiryCheck()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByOwnerAsync();
            _app.Clock.Advance(FiveDays + OneSecond);

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.EXPENSE_NOT_ALLOWED);
        }

        [Fact]
        public async Task Restore_ThenDeleteAgain_CountsTheFiveDaysFromTheSecondDelete()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByMemberAsync();
            _app.Clock.Advance(TimeSpan.FromDays(4));
            await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);
            await ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId);
            _app.Clock.Advance(TimeSpan.FromDays(4));

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Restore_InAGroupThatWasDeleted_Answers404GroupNotFound()
        {
            (ExpenseGroup group, Guid expenseId) = await AddAndDeleteByOwnerAsync();
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        private async Task<(ExpenseGroup Group, Guid ExpenseId)> AddAndDeleteByMemberAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            HttpResponseMessage deleted = await ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

            return (group, expenseId);
        }

        private async Task<(ExpenseGroup Group, Guid ExpenseId)> AddAndDeleteByOwnerAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            HttpResponseMessage deleted = await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

            return (group, expenseId);
        }
    }
}
