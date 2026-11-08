using System.Net;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupGroupTests(CleanupApp app) : CleanupTestBase(app)
    {
        [Fact]
        public async Task Post_GroupDeletedFiftyNineDaysAndTwentyThreeHoursAgo_StaysWithEverythingInIt()
        {
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit - CleanupSteps.OneHour);
            Assert.Equal(FullGroup.ExpectedCounts, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(FullGroup.ExpectedCounts, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));
        }

        [Fact]
        public async Task Post_GroupDeletedFiftyNineDaysAgo_StaysAndIsPastItsRestoreWindow()
        {
            ExpenseGroup group = await CleanupSteps.CreateGroupDeletedAgoAsync(_app, TimeSpan.FromDays(59));

            await _app.RunCleanupAsync();

            HttpResponseMessage response = await GroupRequests.RestoreAsync(group.Owner.Client, group.GroupId);
            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_RESTORE_EXPIRED);
        }

        [Fact]
        public async Task Post_GroupDeletedTwentyNineDaysAgo_StaysAndCanStillBeRestored()
        {
            ExpenseGroup group = await CleanupSteps.CreateGroupDeletedAgoAsync(_app, TimeSpan.FromDays(29));

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(HttpStatusCode.NoContent, (await GroupRequests.RestoreAsync(group.Owner.Client, group.GroupId)).StatusCode);
        }

        [Fact]
        public async Task Post_GroupDeletedExactlySixtyDaysAgo_IsRemoved()
        {
            ExpenseGroup group = await CleanupSteps.CreateGroupDeletedAgoAsync(_app, CleanupSteps.DeletedGroupLimit);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { GroupsRemoved = 1 }, result);
            Assert.Equal(GroupRowCounts.None, await CleanupRows.CountRowsOfGroupAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Post_GroupDeletedNinetyDaysAgo_IsRemoved()
        {
            ExpenseGroup group = await CleanupSteps.CreateGroupDeletedAgoAsync(_app, TimeSpan.FromDays(90));

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(1, result.GroupsRemoved);
            Assert.Equal(GroupRowCounts.None, await CleanupRows.CountRowsOfGroupAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Post_GroupThatWasNeverDeleted_StaysWithEverythingInIt()
        {
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.CreateGroupDeletedAgoAsync(_app, CleanupSteps.DeletedGroupLimit);

            await _app.RunCleanupAsync();

            Assert.Equal(FullGroup.ExpectedCounts, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));
        }

        [Fact]
        public async Task Post_RemovedGroup_TakesEverythingInsideItWithIt()
        {
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit);
            Assert.Equal(FullGroup.ExpectedCounts, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(1, result.GroupsRemoved);
            Assert.Equal(GroupRowCounts.None, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));
        }

        [Fact]
        public async Task Post_RemovedGroup_CountsItsExpensesAndSettlementsInTheTotals()
        {
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { GroupsRemoved = 1, ExpensesRemoved = 2, SettlementsRemoved = 1 }, result);
        }

        [Fact]
        public async Task Post_RemovedGroup_TakesTheDuplicateAnswersAboutItsExpensesButNotTheEntry()
        {
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit);
            Assert.Equal("Same", await DuplicateAnswerRows.AnswerOfAsync(_app, full.Group.Member.UserId, full.MemberEntryId, full.OwnerExpenseId));

            await _app.RunCleanupAsync();

            Assert.Equal(0, await CleanupRows.CountDuplicateAnswersAsync(_app));
            Assert.Contains(full.MemberEntryId, await PersonalEntrySteps.EntryIdsOfMonthAsync(full.Group.Member.Client));
        }

        [Fact]
        public async Task Post_RemovedGroup_LeavesTheAccountsOfItsMembers()
        {
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit);

            await _app.RunCleanupAsync();

            Assert.Equal(1, await DeletionRows.CountUsersWithIdAsync(_app, full.Group.Owner.UserId));
            Assert.Equal(1, await DeletionRows.CountUsersWithIdAsync(_app, full.Group.Member.UserId));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(full.Group.Member.Client)).StatusCode);
        }

        [Fact]
        public async Task Post_RemovedGroup_IsGoneForItsOwner()
        {
            ExpenseGroup group = await CleanupSteps.CreateGroupDeletedAgoAsync(_app, CleanupSteps.DeletedGroupLimit);

            await _app.RunCleanupAsync();

            await ProblemResponse.AssertAsync(await GroupRequests.GetAsync(group.Owner.Client, group.GroupId), HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            await ProblemResponse.AssertAsync(await GroupRequests.RestoreAsync(group.Owner.Client, group.GroupId), HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task Post_GroupWithAnOldDeletedExpenseInsideIt_IsRemovedOnceAndCountsTheExpenseOnce()
        {
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            Guid oldExpenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, full.Group, CleanupSteps.SoftDeletedRowLimit);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(1, result.GroupsRemoved);
            Assert.Equal(3, result.ExpensesRemoved);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "expenses", oldExpenseId));
            Assert.Equal(GroupRowCounts.None, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));
        }

        [Fact]
        public async Task Post_TwoGroupsRemovedAndOneStaying_CountsTwo()
        {
            ExpenseGroup first = await CleanupSteps.CreateGroupDeletedAgoAsync(_app, CleanupSteps.DeletedGroupLimit);
            ExpenseGroup second = await CleanupSteps.CreateGroupDeletedAgoAsync(_app, TimeSpan.FromDays(75));
            ExpenseGroup staying = await CleanupSteps.CreateGroupDeletedAgoAsync(_app, CleanupSteps.DeletedGroupLimit - CleanupSteps.OneHour);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { GroupsRemoved = 2 }, result);
            Assert.Equal(GroupRowCounts.None, await CleanupRows.CountRowsOfGroupAsync(_app, first.GroupId));
            Assert.Equal(GroupRowCounts.None, await CleanupRows.CountRowsOfGroupAsync(_app, second.GroupId));
            Assert.Equal(1, (await CleanupRows.CountRowsOfGroupAsync(_app, staying.GroupId)).Groups);
        }

        [Fact]
        public async Task Post_RemovedGroup_LeavesAnotherGroupOfTheSameMembersUntouched()
        {
            FullGroup removed = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, removed.Group, CleanupSteps.DeletedGroupLimit);
            FullGroup staying = await CleanupSteps.CreateFullGroupAsync(_app);
            string expensesBefore = await ExpenseRows.SnapshotAsync(_app, staying.Group.GroupId);
            string groupBefore = await GroupRows.SnapshotAsync(_app, staying.Group.GroupId);
            string settlementsBefore = await SettlementRows.SnapshotAsync(_app, staying.Group.GroupId);

            await _app.RunCleanupAsync();

            Assert.Equal(expensesBefore, await ExpenseRows.SnapshotAsync(_app, staying.Group.GroupId));
            Assert.Equal(groupBefore, await GroupRows.SnapshotAsync(_app, staying.Group.GroupId));
            Assert.Equal(settlementsBefore, await SettlementRows.SnapshotAsync(_app, staying.Group.GroupId));
        }

        [Fact]
        public async Task Post_GroupThatCannotBeFullyRemoved_Answers500AndKeepsEverythingInIt()
        {
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit);
            await ExpenseRows.BlockAllShareChangesAsync(_app);

            HttpResponseMessage response = await CleanupRequests.PostAsync(_app.CreateClientWithoutCookies(), CleanupApp.Secret);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(FullGroup.ExpectedCounts, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));
        }

        [Fact]
        public async Task Post_AfterAFailedRunOnceTheCauseIsGone_RemovesTheGroupOnTheNextRun()
        {
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit);
            await ExpenseRows.BlockAllShareChangesAsync(_app);
            await CleanupRequests.PostAsync(_app.CreateClientWithoutCookies(), CleanupApp.Secret);
            await ExpenseRows.AllowAllShareChangesAsync(_app);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(1, result.GroupsRemoved);
            Assert.Equal(GroupRowCounts.None, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));
        }

        [Fact]
        public async Task Post_FailureAfterTheAccountPart_KeepsThePurgeThatAlreadyCommitted()
        {
            SignedInUser leaver = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app);
            _app.Clock.Advance(DeletionRequests.GraceTime);
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit);
            await ExpenseRows.BlockAllShareChangesAsync(_app);

            HttpResponseMessage response = await CleanupRequests.PostAsync(_app.CreateClientWithoutCookies(), CleanupApp.Secret);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(_app.Now, (await DeletionRows.ReadAsync(_app, leaver.UserId)).PurgedAt);
        }
    }
}
