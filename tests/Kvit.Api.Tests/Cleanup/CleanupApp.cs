using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Postgres;
using Kvit.Contracts.Cleanup;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupApp(PostgresFixture postgres) : ExpensesApp(postgres)
    {
        public DateTimeOffset Now
        {
            get { return Clock.GetUtcNow(); }
        }

        public async Task ResetAsync()
        {
            Clock.SetTo(ClockStart);
            await ExpenseRows.AllowAllShareChangesAsync(this);
            await CleanupRows.RemoveAllDuplicateAnswersAsync(this);
            await CleanupRows.RemoveSoftDeletedRowsAsync(this);
            await CleanupRows.ForgetPendingDeletionRequestsAsync(this);
            await CleanupRows.DetachAllUsageEventsAsync(this);
        }

        public async Task<CleanupResult> RunCleanupAsync()
        {
            using IServiceScope scope = CreateScope();
            IDataCleanup dataCleanup = scope.ServiceProvider.GetRequiredService<IDataCleanup>();

            CleanupResponse response = await dataCleanup.RunAsync(Now, TestContext.Current.CancellationToken);

            return new CleanupResult(
                response.AccountsPurged,
                response.GroupsRemoved,
                response.ExpensesRemoved,
                response.SettlementsRemoved,
                response.PersonalEntriesRemoved,
                response.UsageEventsDetached);
        }
    }
}
