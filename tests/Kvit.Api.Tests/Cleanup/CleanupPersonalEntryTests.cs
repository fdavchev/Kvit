using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupPersonalEntryTests(CleanupApp app) : CleanupTestBase(app)
    {
        [Fact]
        public async Task Post_EntryDeletedThirtyFourDaysAndTwentyThreeHoursAgo_Stays()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, user, CleanupSteps.SoftDeletedRowLimit - CleanupSteps.OneHour);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(1, await CleanupRows.CountRowsWithIdAsync(_app, "personal_entries", entryId));
        }

        [Fact]
        public async Task Post_EntryDeletedExactlyThirtyFiveDaysAgo_IsRemoved()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, user, CleanupSteps.SoftDeletedRowLimit);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { PersonalEntriesRemoved = 1 }, result);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "personal_entries", entryId));
        }

        [Fact]
        public async Task Post_IncomeDeletedExactlyThirtyFiveDaysAgo_IsRemovedToo()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await CleanupSteps.AddIncomeDeletedAgoAsync(_app, user, CleanupSteps.SoftDeletedRowLimit);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { PersonalEntriesRemoved = 1 }, result);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "personal_entries", entryId));
        }

        [Fact]
        public async Task Post_IncomeDeletedThirtyFourDaysAndTwentyThreeHoursAgo_Stays()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await CleanupSteps.AddIncomeDeletedAgoAsync(_app, user, CleanupSteps.SoftDeletedRowLimit - CleanupSteps.OneHour);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(1, await CleanupRows.CountRowsWithIdAsync(_app, "personal_entries", entryId));
        }

        [Fact]
        public async Task Post_GeneratedIncomeDeletedExactlyThirtyFiveDaysAgo_StaysWhileAManualEntryOfTheSameAgeIsRemoved()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId);
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 8, 15), _app.Now - CleanupSteps.SoftDeletedRowLimit);
            Guid manualEntryId = await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, user, CleanupSteps.SoftDeletedRowLimit);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { PersonalEntriesRemoved = 1 }, result);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "personal_entries", manualEntryId));
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Post_EntryDeletedTwoDaysAgo_StaysAndCanStillBeRestored()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, user, TimeSpan.FromDays(2));

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(HttpStatusCode.NoContent, (await PersonalEntryRequests.RestoreAsync(user.Client, entryId)).StatusCode);
        }

        [Fact]
        public async Task Post_EntryThatWasNeverDeleted_StaysAndStillShowsInItsMonth()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid liveId = await CleanupSteps.AddPersonalEntryAsync(user);
            await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, user, CleanupSteps.SoftDeletedRowLimit);

            await _app.RunCleanupAsync();

            Assert.Equal(liveId, Assert.Single(await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client)));
        }

        [Fact]
        public async Task Post_EntriesRemovedFromTwoPeople_CountsAllOfThemAndNotTheStayingOne()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync();
            await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, first, CleanupSteps.SoftDeletedRowLimit);
            await CleanupSteps.AddIncomeDeletedAgoAsync(_app, first, TimeSpan.FromDays(60));
            await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, second, TimeSpan.FromDays(90));
            await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, second, CleanupSteps.SoftDeletedRowLimit - CleanupSteps.OneHour);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { PersonalEntriesRemoved = 3 }, result);
        }

        [Fact]
        public async Task Post_DuplicateAnswerPointingAtARemovedEntry_IsRemovedAndTheExpenseStays()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await CleanupSteps.AddExpenseAsync(_app, group);
            Guid entryId = await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, group.Member, CleanupSteps.SoftDeletedRowLimit);
            await DuplicateAnswerRows.InsertAsync(_app, group.Member.UserId, entryId, expenseId);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { PersonalEntriesRemoved = 1 }, result);
            Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, expenseId));
            Assert.Equal(HttpStatusCode.OK, (await ExpenseRequests.GetAsync(group.Owner.Client, group.GroupId, expenseId)).StatusCode);
        }

        [Fact]
        public async Task Post_DuplicateAnswerOfAStayingEntry_Stays()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await CleanupSteps.AddExpenseAsync(_app, group);
            Guid entryId = await CleanupSteps.AddPersonalEntryAsync(group.Member);
            await DuplicateAnswerRows.InsertAsync(_app, group.Member.UserId, entryId, expenseId);
            await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, group.Member, CleanupSteps.SoftDeletedRowLimit);

            await _app.RunCleanupAsync();

            Assert.Equal("Same", await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, expenseId));
        }

        [Fact]
        public async Task Post_RemovedEntryOfOnePerson_LeavesTheOtherPersonsEntriesAlone()
        {
            SignedInUser leaver = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync();
            Guid otherEntryId = await CleanupSteps.AddPersonalEntryAsync(other);
            await CleanupSteps.AddPersonalEntryDeletedAgoAsync(_app, leaver, CleanupSteps.SoftDeletedRowLimit);

            await _app.RunCleanupAsync();

            Assert.Equal(otherEntryId, Assert.Single(await PersonalEntrySteps.EntryIdsOfMonthAsync(other.Client)));
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, other.UserId));
        }
    }
}
