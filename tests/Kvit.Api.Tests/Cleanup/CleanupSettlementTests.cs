using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupSettlementTests(CleanupApp app) : CleanupTestBase(app)
    {
        [Fact]
        public async Task Post_SettlementDeletedThirtyFourDaysAndTwentyThreeHoursAgo_Stays()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid settlementId = await CleanupSteps.RecordSettlementDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit - CleanupSteps.OneHour);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            Assert.Equal(1, await CleanupRows.CountRowsWithIdAsync(_app, "settlements", settlementId));
        }

        [Fact]
        public async Task Post_SettlementDeletedExactlyThirtyFiveDaysAgo_IsRemoved()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid settlementId = await CleanupSteps.RecordSettlementDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { SettlementsRemoved = 1 }, result);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "settlements", settlementId));
        }

        [Fact]
        public async Task Post_SettlementDeletedTwentyDaysAgo_StaysAndCanStillBeRestored()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid settlementId = await CleanupSteps.RecordSettlementDeletedAgoAsync(_app, group, TimeSpan.FromDays(20));

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
            await SettlementSteps.RestoreAsync(group.Owner, group.GroupId, settlementId);
        }

        [Fact]
        public async Task Post_SettlementThatWasNeverDeleted_StaysWhileOldOnesAreRemoved()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid liveId = await CleanupSteps.RecordSettlementAsync(group);
            await CleanupSteps.RecordSettlementDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);

            await _app.RunCleanupAsync();

            Assert.Equal(1, await CleanupRows.CountRowsWithIdAsync(_app, "settlements", liveId));
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, liveId, "status"));
        }

        [Fact]
        public async Task Post_SettlementsRemovedFromTwoGroups_CountsAllOfThemAndNotTheStayingOne()
        {
            ExpenseGroup first = await _app.CreateExpenseGroupAsync();
            ExpenseGroup second = await _app.CreateExpenseGroupAsync();
            await CleanupSteps.RecordSettlementDeletedAgoAsync(_app, first, CleanupSteps.SoftDeletedRowLimit);
            await CleanupSteps.RecordSettlementDeletedAgoAsync(_app, first, TimeSpan.FromDays(70));
            await CleanupSteps.RecordSettlementDeletedAgoAsync(_app, second, TimeSpan.FromDays(90));
            await CleanupSteps.RecordSettlementDeletedAgoAsync(_app, second, CleanupSteps.SoftDeletedRowLimit - CleanupSteps.OneHour);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing with { SettlementsRemoved = 3 }, result);
        }

        [Fact]
        public async Task Post_RemovedSettlement_ActivityFeedStillAnswers200WithTheSameEvents()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await CleanupSteps.RecordSettlementDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);
            int eventsBefore = (await SettlementSteps.ReadActivityAsync(group.Owner, group.GroupId)).GetArrayLength();
            Assert.True(eventsBefore >= 3, $"The group should have a created, a recorded and a deleted event, but has {eventsBefore}.");

            await _app.RunCleanupAsync();

            Assert.Equal(eventsBefore, (await SettlementSteps.ReadActivityAsync(group.Owner, group.GroupId)).GetArrayLength());
            Assert.Equal(eventsBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Post_RemovedSettlement_BalancesOfTheGroupStayTheSame()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await CleanupSteps.AddExpenseAsync(_app, group);
            await CleanupSteps.RecordSettlementDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);
            List<string> before = await DeletionSteps.BalancesAsync(group.Owner.Client, group.GroupId);
            Assert.NotEmpty(before);

            await _app.RunCleanupAsync();

            Assert.Equal(before, await DeletionSteps.BalancesAsync(group.Owner.Client, group.GroupId));
        }

    }
}
