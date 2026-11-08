using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeAtomicityTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Purge_WhenTheLastStepFails_ThrowsTheRealErrorAndLeavesEverythingAsItWas()
        {
            SignedInUser user = await _app.SignUpAsync("Zorica");
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid expenseId = await DeletionGroups.CreateNeutralExpenseAsync(_app);
            await PersonalDataSteps.CreateFullSetAsync(_app, user, expenseId);
            await GroupRows.InsertMemberAsync(_app, group.GroupId, user.UserId, "Zorica", group.Owner.UserId, joinedAt: DeletionGroups.JoinedAt(30));
            DateTimeOffset now = _app.Clock.GetUtcNow() + DeletionRequests.GraceTime;
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.FormerMember);
            string userBefore = await DeletionRows.SnapshotOfUserRowAsync(_app, user.UserId);
            string rowsBefore = await DeletionRows.SnapshotOfRowsOfUserAsync(_app, user.UserId);
            string tablesBefore = await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app);
            await BlockPurgedUsersAsync();

            Exception error;
            try
            {
                error = await Assert.ThrowsAnyAsync<Exception>(() => AccountPurges.PurgeAsync(_app, user.UserId, DeletionRequests.FormerMember, now));
            }
            finally
            {
                await _app.ExecuteAsync("DROP TRIGGER IF EXISTS block_purge ON users");
            }

            Assert.Contains(ExceptionChain.Of(error), exception => exception.Message.Contains("purge is blocked by the test", StringComparison.Ordinal));
            Assert.Equal(userBefore, await DeletionRows.SnapshotOfUserRowAsync(_app, user.UserId));
            Assert.Equal(rowsBefore, await DeletionRows.SnapshotOfRowsOfUserAsync(_app, user.UserId));
            Assert.Equal(tablesBefore, await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app));
            Assert.Equal(PersonalCounts.Full, await DeletionRows.PersonalCountsAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Purge_AfterAFailedAttempt_TheNextAttemptSucceeds()
        {
            SignedInUser user = await _app.SignUpAsync("Zorica");
            DateTimeOffset now = _app.Clock.GetUtcNow() + DeletionRequests.GraceTime;
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            await BlockPurgedUsersAsync();
            try
            {
                await Assert.ThrowsAnyAsync<Exception>(() => AccountPurges.PurgeAsync(_app, user.UserId, DeletionRequests.KeepName, now));
            }
            finally
            {
                await _app.ExecuteAsync("DROP TRIGGER IF EXISTS block_purge ON users");
            }

            await AccountPurges.PurgeAsync(_app, user.UserId, DeletionRequests.KeepName, now);

            Assert.Equal(now, (await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        private Task BlockPurgedUsersAsync()
        {
            return _app.ExecuteAsync(
                "CREATE OR REPLACE FUNCTION block_purge() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.purged_at IS NOT NULL THEN RAISE EXCEPTION 'purge is blocked by the test'; END IF; RETURN NEW; END $$; "
                + "DROP TRIGGER IF EXISTS block_purge ON users; "
                + "CREATE TRIGGER block_purge BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION block_purge();");
        }
    }
}
