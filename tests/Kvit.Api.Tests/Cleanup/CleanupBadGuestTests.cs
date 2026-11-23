using System.Net;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Contracts.Cleanup;
using Kvit.Domain.Cleanup;
using Kvit.Infrastructure.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupBadGuestTests(LazyCleanupApp _app) : IClassFixture<LazyCleanupApp>, IAsyncLifetime
    {
        private const string RefusingTrigger = "test_refuse_purging_user";
        private static readonly TimeSpan LeftBehindTime = TimeSpan.FromDays(CleanupRules.LeftBehindGuestDays);

        public ValueTask InitializeAsync()
        {
            return new ValueTask(_app.ResetAsync());
        }

        public async ValueTask DisposeAsync()
        {
            await _app.ExecuteAsync($"DROP TRIGGER IF EXISTS {RefusingTrigger} ON users; DROP FUNCTION IF EXISTS {RefusingTrigger}();");
        }

        [Fact]
        public async Task Run_OneGuestThatCannotBePurged_PurgesTheOtherGuestRunsTheLaterStepsLogsTheGuestAndStillFails()
        {
            (ExpenseGroup goodGroup, GuestMember goodGuest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            (ExpenseGroup badGroup, GuestMember badGuest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Ana");
            DateTimeOffset createdAt = _app.Now;
            await LetInAgainAsync(goodGroup, goodGuest);
            await LetInAgainAsync(badGroup, badGuest);
            Guid dueExpenseId = await CleanupSteps.AddExpenseDeletedAgoAsync(_app, goodGroup, CleanupSteps.SoftDeletedRowLimit);
            await RefuseToPurgeAsync(badGuest.UserId);
            DateTimeOffset runAt = createdAt + LeftBehindTime;

            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => RunAtAsync(runAt));

            Assert.Contains(badGuest.UserId.ToString(), failure.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(goodGuest.UserId.ToString(), failure.Message, StringComparison.Ordinal);
            Assert.Equal(runAt, (await DeletionRows.ReadAsync(_app, goodGuest.UserId)).PurgedAt);
            Assert.Null((await DeletionRows.ReadAsync(_app, badGuest.UserId)).PurgedAt);
            Assert.Equal(0, await CleanupRows.CountRowsWithIdAsync(_app, "expenses", dueExpenseId));
            RecordedLogEntry error = Assert.Single(_app.Logs.EntriesOf<AccountPurger>(), entry => entry.Level == LogLevel.Error);
            Assert.Contains(badGuest.UserId.ToString(), error.Message, StringComparison.Ordinal);
            Assert.NotNull(error.Exception);
        }

        private async Task RefuseToPurgeAsync(Guid userId)
        {
            await _app.ExecuteAsync(
                $"""
                CREATE FUNCTION {RefusingTrigger}() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF OLD.id = '{userId}' THEN
                        RAISE EXCEPTION 'The test refuses to purge user %', OLD.id;
                    END IF;
                    RETURN NEW;
                END
                $$;
                CREATE TRIGGER {RefusingTrigger} BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION {RefusingTrigger}();
                """);
        }

        private async Task LetInAgainAsync(ExpenseGroup group, GuestMember guest)
        {
            HttpResponseMessage response = await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, guest.RowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private async Task<CleanupResponse> RunAtAsync(DateTimeOffset now)
        {
            _app.Clock.SetTo(now);
            using IServiceScope scope = _app.CreateScope();
            IDataCleanup dataCleanup = scope.ServiceProvider.GetRequiredService<IDataCleanup>();

            return await dataCleanup.RunAsync(now, TestContext.Current.CancellationToken);
        }
    }
}
