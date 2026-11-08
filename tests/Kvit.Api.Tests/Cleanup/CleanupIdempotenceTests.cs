using Kvit.Api.Tests.Expenses;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupIdempotenceTests(CleanupApp app) : CleanupTestBase(app)
    {
        [Fact]
        public async Task Post_EveryKindDue_CountsExactlyOneOfEachKind()
        {
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.OneOfEveryKind, result);
        }

        [Fact]
        public async Task Post_SecondCallRightAfterTheFirst_AnswersSixZeros()
        {
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);
            await _app.RunCleanupAsync();

            CleanupResult second = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, second);
        }

        [Fact]
        public async Task Post_SecondCallRightAfterTheFirst_ChangesNothing()
        {
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);
            await _app.RunCleanupAsync();
            string afterTheFirst = await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app);

            await _app.RunCleanupAsync();

            Assert.Equal(afterTheFirst, await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app));
        }

        [Fact]
        public async Task Post_ThreeCallsInARow_OnlyTheFirstCountsAnything()
        {
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);

            CleanupResult first = await _app.RunCleanupAsync();
            CleanupResult second = await _app.RunCleanupAsync();
            CleanupResult third = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.OneOfEveryKind, first);
            Assert.Equal(CleanupResult.Nothing, second);
            Assert.Equal(CleanupResult.Nothing, third);
        }

        [Fact]
        public async Task Post_NothingDue_ChangesNothingAnywhere()
        {
            await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.CreateAccountRequestingDeletionAsync(_app);
            string before = await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(before, await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app));
        }

        [Fact]
        public async Task Post_LaterRunAfterMoreTimeHasPassed_RemovesWhatBecameDueInTheMeantime()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, TimeSpan.FromDays(30));
            Assert.Equal(CleanupResult.Nothing, await _app.RunCleanupAsync());
            _app.Clock.Advance(TimeSpan.FromDays(5));

            CleanupResult later = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { ExpensesRemoved = 1 }, later);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "expenses", expenseId));
        }
    }
}
