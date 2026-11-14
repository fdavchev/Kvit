using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeTransactionTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Purge_WithoutAnOpenTransaction_ThrowsAClearErrorAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync("Zorica");
            string userBefore = await DeletionRows.SnapshotOfUserRowAsync(_app, user.UserId);
            using IServiceScope scope = _app.CreateScope();
            IAccountPurger purger = AccountPurges.ResolveFrom(scope);

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => purger.PurgeAccountAsync(user.UserId, DeletionNameChoice.FormerMember, _app.Clock.GetUtcNow(), TestContext.Current.CancellationToken));

            Assert.Contains("outside a transaction", error.Message, StringComparison.Ordinal);
            Assert.Equal(userBefore, await DeletionRows.SnapshotOfUserRowAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Purge_InsideACallersTransactionThatIsRolledBack_LeavesEverythingAsItWas()
        {
            SignedInUser user = await _app.SignUpAsync("Zorica");
            string userBefore = await DeletionRows.SnapshotOfUserRowAsync(_app, user.UserId);
            string tablesBefore = await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app);
            using IServiceScope scope = _app.CreateScope();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            IAccountPurger purger = AccountPurges.ResolveFrom(scope);

            await unitOfWork.OpenTransactionAsync(TestContext.Current.CancellationToken);
            bool purged = await purger.PurgeAccountAsync(user.UserId, DeletionNameChoice.FormerMember, _app.Clock.GetUtcNow(), TestContext.Current.CancellationToken);
            await unitOfWork.RollbackAsync(TestContext.Current.CancellationToken);

            Assert.True(purged);
            Assert.Equal(userBefore, await DeletionRows.SnapshotOfUserRowAsync(_app, user.UserId));
            Assert.Equal(tablesBefore, await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app));
        }

        [Fact]
        public async Task Purge_OfAnAccountThatIsAlreadyPurged_AnswersFalse()
        {
            SignedInUser user = await _app.SignUpAsync("Zorica");
            DateTimeOffset now = _app.Clock.GetUtcNow();
            await AccountPurges.PurgeAsync(_app, user.UserId, DeletionRequests.FormerMember, now);
            using IServiceScope scope = _app.CreateScope();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            IAccountPurger purger = AccountPurges.ResolveFrom(scope);

            await unitOfWork.OpenTransactionAsync(TestContext.Current.CancellationToken);
            bool purged = await purger.PurgeAccountAsync(user.UserId, DeletionNameChoice.FormerMember, now, TestContext.Current.CancellationToken);
            await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

            Assert.False(purged);
        }
    }
}
