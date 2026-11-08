using System.Net;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupExpenseTests(CleanupApp app) : CleanupTestBase(app)
    {
        [Fact]
        public async Task Post_ExpenseDeletedThirtyFourDaysAndTwentyThreeHoursAgo_Stays()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit - CleanupSteps.OneHour);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(1, await CleanupRows.CountRowsWithIdAsync(_app, "expenses", expenseId));
        }

        [Fact]
        public async Task Post_ExpenseDeletedExactlyThirtyFiveDaysAgo_IsRemoved()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { ExpensesRemoved = 1 }, result);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "expenses", expenseId));
        }

        [Fact]
        public async Task Post_ExpenseDeletedFortyDaysAgo_IsRemoved()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, TimeSpan.FromDays(40));

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(1, result.ExpensesRemoved);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "expenses", expenseId));
        }

        [Fact]
        public async Task Post_ExpenseDeletedTwoDaysAgo_StaysAndCanStillBeRestored()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, TimeSpan.FromDays(2));

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId)).StatusCode);
        }

        [Fact]
        public async Task Post_ExpenseThatWasNeverDeleted_StaysWhileOldOnesAreRemoved()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid liveId = await CleanupSteps.AddExpenseAsync(_app, group);
            await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);

            await _app.RunCleanupAsync();

            Assert.Equal(1, await CleanupRows.CountRowsWithIdAsync(_app, "expenses", liveId));
            Assert.Equal(HttpStatusCode.OK, (await ExpenseRequests.GetAsync(group.Owner.Client, group.GroupId, liveId)).StatusCode);
        }

        [Fact]
        public async Task Post_RemovedExpense_TakesItsSharesWithIt()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);
            Assert.Equal(2, await ExpenseRows.CountSharesOfAsync(_app, expenseId));

            await _app.RunCleanupAsync();

            Assert.Equal(0, await ExpenseRows.CountSharesOfAsync(_app, expenseId));
        }

        [Fact]
        public async Task Post_RemovedExpense_LeavesTheSharesOfTheStayingExpenses()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid liveId = await CleanupSteps.AddExpenseAsync(_app, group);
            await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);

            await _app.RunCleanupAsync();

            Assert.Equal(2, await ExpenseRows.CountSharesOfAsync(_app, liveId));
        }

        [Fact]
        public async Task Post_RemovedExpenseOfAGroup_GetAnswers404()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);

            await _app.RunCleanupAsync();

            Assert.Equal(HttpStatusCode.NotFound, (await ExpenseRequests.GetAsync(group.Owner.Client, group.GroupId, expenseId)).StatusCode);
        }

        [Fact]
        public async Task Post_ExpensesRemovedFromTwoGroups_CountsAllOfThemAndNotTheStayingOne()
        {
            ExpenseGroup first = await _app.CreateExpenseGroupAsync();
            ExpenseGroup second = await _app.CreateExpenseGroupAsync();
            await CleanupSteps.AddExpenseDeletedAgoAsync(_app, first, CleanupSteps.SoftDeletedRowLimit);
            await CleanupSteps.AddExpenseDeletedAgoAsync(_app, first, TimeSpan.FromDays(50));
            await CleanupSteps.AddExpenseDeletedAgoAsync(_app, second, TimeSpan.FromDays(90));
            await CleanupSteps.AddExpenseDeletedAgoAsync(_app, second, CleanupSteps.SoftDeletedRowLimit - CleanupSteps.OneHour);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { ExpensesRemoved = 3 }, result);
        }

        [Fact]
        public async Task Post_RemovedExpense_ActivityFeedStillAnswers200WithTheSameEvents()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);
            int eventsBefore = (await SettlementSteps.ReadActivityAsync(group.Owner, group.GroupId)).GetArrayLength();
            Assert.True(eventsBefore >= 3, $"The group should have a created, an added and a deleted event, but has {eventsBefore}.");

            await _app.RunCleanupAsync();

            Assert.Equal(eventsBefore, (await SettlementSteps.ReadActivityAsync(group.Owner, group.GroupId)).GetArrayLength());
            Assert.Equal(eventsBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Post_RemovedExpense_BalancesOfTheGroupStayTheSame()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await CleanupSteps.AddExpenseAsync(_app, group);
            await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);
            List<string> before = await DeletionSteps.BalancesAsync(group.Owner.Client, group.GroupId);
            Assert.NotEmpty(before);

            await _app.RunCleanupAsync();

            Assert.Equal(before, await DeletionSteps.BalancesAsync(group.Owner.Client, group.GroupId));
        }

        [Fact]
        public async Task Post_DuplicateAnswerPointingAtARemovedExpense_IsRemovedAndTheEntryStays()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);
            Guid entryId = await CleanupSteps.AddPersonalEntryAsync(group.Member);
            await DuplicateAnswerRows.InsertAsync(_app, group.Member.UserId, entryId, expenseId);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { ExpensesRemoved = 1 }, result);
            Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, expenseId));
            Assert.Contains(entryId, await PersonalEntrySteps.EntryIdsOfMonthAsync(group.Member.Client));
        }

        [Fact]
        public async Task Post_DuplicateAnswerPointingAtAStayingExpense_Stays()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid liveId = await CleanupSteps.AddExpenseAsync(_app, group);
            await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);
            Guid entryId = await CleanupSteps.AddPersonalEntryAsync(group.Member);
            await DuplicateAnswerRows.InsertAsync(_app, group.Member.UserId, entryId, liveId);

            await _app.RunCleanupAsync();

            Assert.Equal("Same", await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, liveId));
        }
    }
}
