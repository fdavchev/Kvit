using System.Net;
using Kvit.Api.Tests.Dashboard;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Infrastructure.Cleanup;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class LazyCleanupOnActiveTests(LazyCleanupApp _app) : IClassFixture<LazyCleanupApp>
    {
        private static readonly TimeSpan AnswerLimit = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan NoNewRunWaitTime = TimeSpan.FromSeconds(1);

        [Fact]
        public async Task Active_FirstOfTheUtcDay_RemovesDueRowsInTheBackground()
        {
            ExpenseGroup group = await StartANewDayWithAGroupAsync();
            Guid dueExpenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);
            int runsBefore = _app.CountLazyRunsThatEnded();

            HttpResponseMessage response = await DashboardRequests.ActiveAsync(group.Owner.Client);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await _app.WaitUntilLazyRunsEndedAsync(runsBefore + 1);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "expenses", dueExpenseId));
        }

        [Fact]
        public async Task Active_FirstOfTheUtcDay_AnswersWhileTheCleanupIsStillWaitingForTheDatabase()
        {
            ExpenseGroup group = await StartANewDayWithAGroupAsync();
            Guid dueExpenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);
            int runsBefore = _app.CountLazyRunsThatEnded();
            await using NpgsqlConnection lockHolder = await _app.OpenConnectionAsync();
            await using NpgsqlTransaction lockTransaction = await lockHolder.BeginTransactionAsync(TestContext.Current.CancellationToken);
            await using (NpgsqlCommand lockExpenses = new("LOCK TABLE expenses IN ACCESS EXCLUSIVE MODE", lockHolder, lockTransaction))
            {
                await lockExpenses.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            }

            HttpResponseMessage response = await DashboardRequests.ActiveAsync(group.Owner.Client).WaitAsync(AnswerLimit, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await _app.WaitUntilAsync(IsSomethingWaitingForTheExpensesLockAsync, "the lazy cleanup to wait for the locked expenses table");
            Assert.Equal(runsBefore, _app.CountLazyRunsThatEnded());
            await lockTransaction.RollbackAsync(TestContext.Current.CancellationToken);
            await _app.WaitUntilLazyRunsEndedAsync(runsBefore + 1);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "expenses", dueExpenseId));
        }

        [Fact]
        public async Task Active_AfterTheLazyRunFailed_LogsTheErrorAndTheNextActiveOfTheSameDayRunsItAgain()
        {
            _app.Clock.Advance(TimeSpan.FromDays(1));
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit);
            await ExpenseRows.BlockAllShareChangesAsync(_app);
            int runsBefore = _app.CountLazyRunsThatEnded();
            int errorsBefore = CountLazyErrors();

            try
            {
                Assert.Equal(HttpStatusCode.NoContent, (await DashboardRequests.ActiveAsync(full.Group.Owner.Client)).StatusCode);
                await _app.WaitUntilLazyRunsEndedAsync(runsBefore + 1);
            }
            finally
            {
                await ExpenseRows.AllowAllShareChangesAsync(_app);
            }

            RecordedLogEntry error = Assert.Single(_app.Logs.EntriesOf<LazyDailyCleanup>().Where(entry => entry.Level == LogLevel.Error).Skip(errorsBefore));
            Assert.NotNull(error.Exception);
            Assert.Equal(FullGroup.ExpectedCounts, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));

            _app.Clock.Advance(LazyDailyCleanup.WaitAfterFailure);
            Assert.Equal(HttpStatusCode.NoContent, (await DashboardRequests.ActiveAsync(full.Group.Member.Client)).StatusCode);
            await _app.WaitUntilLazyRunsEndedAsync(runsBefore + 2);

            Assert.Equal(GroupRowCounts.None, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));
        }

        [Fact]
        public async Task Active_AfterTheLazyRunFailed_TheSamePersonsNextActiveOfTheSameDayRunsItAgain()
        {
            _app.Clock.Advance(TimeSpan.FromDays(1));
            FullGroup full = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.SetGroupDeletedAgoAsync(_app, full.Group, CleanupSteps.DeletedGroupLimit);
            await ExpenseRows.BlockAllShareChangesAsync(_app);
            int runsBefore = _app.CountLazyRunsThatEnded();
            int errorsBefore = CountLazyErrors();

            try
            {
                Assert.Equal(HttpStatusCode.NoContent, (await DashboardRequests.ActiveAsync(full.Group.Owner.Client)).StatusCode);
                await _app.WaitUntilLazyRunsEndedAsync(runsBefore + 1);
            }
            finally
            {
                await ExpenseRows.AllowAllShareChangesAsync(_app);
            }

            Assert.Equal(1, CountLazyErrors() - errorsBefore);
            Assert.Equal(FullGroup.ExpectedCounts, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));

            _app.Clock.Advance(LazyDailyCleanup.WaitAfterFailure);
            Assert.Equal(HttpStatusCode.NoContent, (await DashboardRequests.ActiveAsync(full.Group.Owner.Client)).StatusCode);
            await _app.WaitUntilLazyRunsEndedAsync(runsBefore + 2);

            Assert.Equal(GroupRowCounts.None, await CleanupRows.CountRowsOfGroupAsync(_app, full.Group.GroupId));
            Assert.Equal(1, CountLazyErrors() - errorsBefore);
        }

        [Fact]
        public async Task Active_OfAPersonAlreadyCountedTodayAfterTheRunFinished_StartsNoNewRun()
        {
            ExpenseGroup group = await StartANewDayWithAGroupAsync();
            int runsBefore = _app.CountLazyRunsThatEnded();
            Assert.Equal(HttpStatusCode.NoContent, (await DashboardRequests.ActiveAsync(group.Owner.Client)).StatusCode);
            await _app.WaitUntilLazyRunsEndedAsync(runsBefore + 1);
            Guid dueExpenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, group, CleanupSteps.SoftDeletedRowLimit);

            HttpResponseMessage response = await DashboardRequests.ActiveAsync(group.Owner.Client);
            await Task.Delay(NoNewRunWaitTime, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(runsBefore + 1, _app.CountLazyRunsThatEnded());
            Assert.Equal(1, await CleanupRows.CountRowsWithIdAsync(_app, "expenses", dueExpenseId));
        }

        private async Task<ExpenseGroup> StartANewDayWithAGroupAsync()
        {
            _app.Clock.Advance(TimeSpan.FromDays(1));

            return await _app.CreateExpenseGroupAsync();
        }

        private int CountLazyErrors()
        {
            return _app.Logs.EntriesOf<LazyDailyCleanup>().Count(entry => entry.Level == LogLevel.Error);
        }

        private async Task<bool> IsSomethingWaitingForTheExpensesLockAsync()
        {
            List<string?> waiting = await _app.QueryAsync(
                "SELECT count(*)::text FROM pg_locks waiting JOIN pg_class locked ON locked.oid = waiting.relation "
                + "WHERE locked.relname = 'expenses' AND NOT waiting.granted "
                + "AND waiting.database = (SELECT oid FROM pg_database WHERE datname = current_database())");

            return int.Parse(Assert.Single(waiting)!) > 0;
        }
    }
}
