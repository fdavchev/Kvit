using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeDueTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly DateTimeOffset RequestTime = new(2026, 11, 4, 9, 30, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset FarFuture = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task PurgeDue_RequestedTwentyNineDaysAndTwentyThreeHoursAgo_PurgesNothing()
        {
            await FlushAsync();
            SignedInUser user = await RequestAtAsync(RequestTime, DeletionRequests.KeepName);

            int purged = await AccountPurges.PurgeDueAsync(_app, RequestTime + DeletionRequests.GraceTime - TimeSpan.FromHours(1));

            Assert.Equal(0, purged);
            Assert.Null((await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task PurgeDue_RequestedOneSecondLessThanThirtyDaysAgo_PurgesNothing()
        {
            await FlushAsync();
            SignedInUser user = await RequestAtAsync(RequestTime, DeletionRequests.KeepName);

            int purged = await AccountPurges.PurgeDueAsync(_app, RequestTime + DeletionRequests.GraceTime - TimeSpan.FromSeconds(1));

            Assert.Equal(0, purged);
            Assert.Null((await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task PurgeDue_RequestedExactlyThirtyDaysAgo_PurgesIt()
        {
            await FlushAsync();
            SignedInUser user = await RequestAtAsync(RequestTime, DeletionRequests.KeepName);
            DateTimeOffset now = RequestTime + DeletionRequests.GraceTime;

            int purged = await AccountPurges.PurgeDueAsync(_app, now);

            Assert.Equal(1, purged);
            Assert.Equal(now, (await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task PurgeDue_RequestedThirtyOneDaysAgo_PurgesIt()
        {
            await FlushAsync();
            SignedInUser user = await RequestAtAsync(RequestTime, DeletionRequests.FormerMember);

            int purged = await AccountPurges.PurgeDueAsync(_app, RequestTime + TimeSpan.FromDays(31));

            Assert.Equal(1, purged);
            Assert.NotNull((await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task PurgeDue_NothingRequested_ReturnsZero()
        {
            await FlushAsync();
            await _app.SignUpAsync();

            int purged = await AccountPurges.PurgeDueAsync(_app, FarFuture);

            Assert.Equal(0, purged);
        }

        [Fact]
        public async Task PurgeDue_AccountThatNeverRequestedADeletion_IsNeverPurged()
        {
            await FlushAsync();
            SignedInUser user = await _app.SignUpAsync();

            await AccountPurges.PurgeDueAsync(_app, FarFuture);

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, user.UserId));
            Assert.Equal(user.Form.Email, await DeletionRows.UserValueAsync(_app, user.UserId, "email"));
        }

        [Fact]
        public async Task PurgeDue_TwoDueAndOneNotYetDue_ReturnsTwoAndLeavesTheOtherPending()
        {
            await FlushAsync();
            SignedInUser firstDue = await RequestAtAsync(RequestTime, DeletionRequests.KeepName);
            SignedInUser notDue = await RequestAtAsync(RequestTime + TimeSpan.FromDays(1), DeletionRequests.KeepName);
            SignedInUser secondDue = await RequestAtAsync(RequestTime, DeletionRequests.FormerMember);

            int purged = await AccountPurges.PurgeDueAsync(_app, RequestTime + DeletionRequests.GraceTime);

            Assert.Equal(2, purged);
            Assert.NotNull((await DeletionRows.ReadAsync(_app, firstDue.UserId)).PurgedAt);
            Assert.NotNull((await DeletionRows.ReadAsync(_app, secondDue.UserId)).PurgedAt);
            Assert.Null((await DeletionRows.ReadAsync(_app, notDue.UserId)).PurgedAt);
            Assert.Equal(notDue.Form.Email, await DeletionRows.UserValueAsync(_app, notDue.UserId, "email"));
        }

        [Fact]
        public async Task PurgeDue_UsesTheStoredChoiceOfEachAccount()
        {
            await FlushAsync();
            SignedInUser keeper = await _app.SignUpAsync("Keeper");
            SignedInUser leaver = await _app.SignUpAsync("Leaver");
            _app.Clock.SetTo(RequestTime);
            await DeletionSteps.RequestAsync(keeper.Client, DeletionRequests.KeepName);
            await DeletionSteps.RequestAsync(leaver.Client, DeletionRequests.FormerMember);

            await AccountPurges.PurgeDueAsync(_app, RequestTime + DeletionRequests.GraceTime);

            Assert.Equal("Keeper", await DeletionRows.UserValueAsync(_app, keeper.UserId, "display_name"));
            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.UserValueAsync(_app, leaver.UserId, "display_name"));
        }

        [Fact]
        public async Task PurgeDue_SecondCallRightAfterwards_ReturnsZeroAndChangesNothing()
        {
            await FlushAsync();
            await RequestAtAsync(RequestTime, DeletionRequests.KeepName);
            DateTimeOffset now = RequestTime + DeletionRequests.GraceTime;
            await AccountPurges.PurgeDueAsync(_app, now);
            string before = await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app);

            int second = await AccountPurges.PurgeDueAsync(_app, now + TimeSpan.FromDays(1));

            Assert.Equal(0, second);
            Assert.Equal(before, await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app));
        }

        [Fact]
        public async Task PurgeDue_AlreadyPurgedAccount_KeepsItsFirstPurgeTime()
        {
            await FlushAsync();
            SignedInUser user = await RequestAtAsync(RequestTime, DeletionRequests.KeepName);
            DateTimeOffset firstNow = RequestTime + DeletionRequests.GraceTime;
            await AccountPurges.PurgeDueAsync(_app, firstNow);

            await AccountPurges.PurgeDueAsync(_app, firstNow + TimeSpan.FromDays(5));

            Assert.Equal(firstNow, (await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task PurgeDue_DeletionCancelledByLogIn_IsSkipped()
        {
            await FlushAsync();
            SignedInUser user = await RequestAtAsync(RequestTime, DeletionRequests.KeepName);
            await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password);

            int purged = await AccountPurges.PurgeDueAsync(_app, FarFuture);

            Assert.Equal(0, purged);
            Assert.Equal(user.Form.Email, await DeletionRows.UserValueAsync(_app, user.UserId, "email"));
        }

        [Fact]
        public async Task PurgeDue_GuestRequestedThirtyDaysAgo_IsPurged()
        {
            await FlushAsync();
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            _app.Clock.SetTo(RequestTime);
            await DeletionSteps.RequestAsync(guest.Client, DeletionRequests.KeepName);

            int purged = await AccountPurges.PurgeDueAsync(_app, RequestTime + DeletionRequests.GraceTime);

            Assert.Equal(1, purged);
            Assert.NotNull((await DeletionRows.ReadAsync(_app, guest.UserId)).PurgedAt);
        }

        [Fact]
        public async Task PurgeDue_StoredRequestTimeAndChoice_StayAsEvidenceAfterThePurge()
        {
            await FlushAsync();
            SignedInUser user = await RequestAtAsync(RequestTime, DeletionRequests.FormerMember);
            DateTimeOffset now = RequestTime + DeletionRequests.GraceTime;

            await AccountPurges.PurgeDueAsync(_app, now);

            Assert.Equal(new StoredDeletion(RequestTime, DeletionRequests.FormerMember, now), await DeletionRows.ReadAsync(_app, user.UserId));
        }

        private async Task FlushAsync()
        {
            await AccountPurges.PurgeDueAsync(_app, FarFuture);
        }

        private async Task<SignedInUser> RequestAtAsync(DateTimeOffset requestedAt, string nameChoice)
        {
            SignedInUser user = await _app.SignUpAsync();
            _app.Clock.SetTo(requestedAt);
            await DeletionSteps.RequestAsync(user.Client, nameChoice);

            return user;
        }
    }
}
