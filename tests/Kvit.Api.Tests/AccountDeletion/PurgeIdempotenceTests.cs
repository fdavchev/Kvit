using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeIdempotenceTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData("KeepName")]
        [InlineData("FormerMember")]
        public async Task Purge_CalledASecondTimeForTheSameAccount_ChangesNothingAndThrowsNothing(string nameChoice)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, group.Owner.Client, group.Owner.UserId, nameChoice);
            string tablesBefore = await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app);
            string userBefore = await DeletionRows.SnapshotOfUserRowAsync(_app, group.Owner.UserId);

            await AccountPurges.PurgeAsync(_app, group.Owner.UserId, nameChoice, now + TimeSpan.FromDays(1));

            Assert.Equal(tablesBefore, await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app));
            Assert.Equal(userBefore, await DeletionRows.SnapshotOfUserRowAsync(_app, group.Owner.UserId));
        }

        [Fact]
        public async Task Purge_CalledASecondTimeWithTheOtherChoice_ChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.KeepName);
            string tablesBefore = await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app);
            string userBefore = await DeletionRows.SnapshotOfUserRowAsync(_app, group.Member.UserId);

            await AccountPurges.PurgeAsync(_app, group.Member.UserId, DeletionRequests.FormerMember, now + TimeSpan.FromDays(1));

            Assert.Equal(tablesBefore, await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app));
            Assert.Equal(userBefore, await DeletionRows.SnapshotOfUserRowAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Purge_CalledASecondTimeForAnOwner_DoesNotWriteAnotherEvent()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser heir = await _app.SignUpAsync("Heir");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, heir, 10);
            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, owner.Client, owner.UserId, DeletionRequests.KeepName);

            await AccountPurges.PurgeAsync(_app, owner.UserId, DeletionRequests.KeepName, now + TimeSpan.FromDays(1));

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "OwnershipTransferred"));
        }

        [Fact]
        public async Task PurgeDue_AfterAForcedPurgeOfTheSameAccount_CountsItAsNotDue()
        {
            SignedInUser user = await _app.SignUpAsync();
            await AccountPurges.PurgeDueAsync(_app, new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            int purged = await AccountPurges.PurgeDueAsync(_app, now + TimeSpan.FromDays(60));

            Assert.Equal(0, purged);
        }
    }
}
