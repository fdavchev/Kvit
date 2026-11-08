using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupAccountTests(CleanupApp app) : CleanupTestBase(app)
    {
        private static readonly DateTimeOffset FarFuture = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Post_RequestedTwentyNineDaysAndTwentyThreeHoursAgo_PurgesNothing()
        {
            SignedInUser user = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app);
            DateTimeOffset requestedAt = _app.Now;

            CleanupResult result = await RunAtAsync(requestedAt + DeletionRequests.GraceTime - CleanupSteps.OneHour);

            Assert.Equal(0, result.AccountsPurged);
            Assert.Null((await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task Post_RequestedOneSecondLessThanThirtyDaysAgo_PurgesNothing()
        {
            SignedInUser user = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app);
            DateTimeOffset requestedAt = _app.Now;

            CleanupResult result = await RunAtAsync(requestedAt + DeletionRequests.GraceTime - CleanupSteps.OneSecond);

            Assert.Equal(0, result.AccountsPurged);
            Assert.Null((await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task Post_RequestedExactlyThirtyDaysAgo_PurgesItAtTheClockTime()
        {
            SignedInUser user = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app);
            DateTimeOffset now = _app.Now + DeletionRequests.GraceTime;

            CleanupResult result = await RunAtAsync(now);

            Assert.Equal(1, result.AccountsPurged);
            Assert.Equal(now, (await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task Post_RequestedThirtyOneDaysAgo_PurgesIt()
        {
            SignedInUser user = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app, DeletionRequests.FormerMember);

            CleanupResult result = await RunAtAsync(_app.Now + DeletionRequests.GraceTime + TimeSpan.FromDays(1));

            Assert.Equal(1, result.AccountsPurged);
            Assert.NotNull((await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task Post_TwoDueAndOneNotYetDue_CountsTwoAndLeavesTheOtherPending()
        {
            SignedInUser firstDue = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app);
            SignedInUser secondDue = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app, DeletionRequests.FormerMember);
            DateTimeOffset dueAt = _app.Now + DeletionRequests.GraceTime;
            _app.Clock.Advance(TimeSpan.FromDays(1));
            SignedInUser notDue = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app);

            CleanupResult result = await RunAtAsync(dueAt);

            Assert.Equal(2, result.AccountsPurged);
            Assert.NotNull((await DeletionRows.ReadAsync(_app, firstDue.UserId)).PurgedAt);
            Assert.NotNull((await DeletionRows.ReadAsync(_app, secondDue.UserId)).PurgedAt);
            Assert.Null((await DeletionRows.ReadAsync(_app, notDue.UserId)).PurgedAt);
            Assert.Equal(notDue.Form.Email, await DeletionRows.UserValueAsync(_app, notDue.UserId, "email"));
        }

        [Fact]
        public async Task Post_AccountThatNeverAskedToBeDeleted_IsNeverPurged()
        {
            SignedInUser user = await _app.SignUpAsync();

            CleanupResult result = await RunAtAsync(FarFuture);

            Assert.Equal(0, result.AccountsPurged);
            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, user.UserId));
            Assert.Equal(user.Form.Email, await DeletionRows.UserValueAsync(_app, user.UserId, "email"));
        }

        [Fact]
        public async Task Post_DeletionCancelledByLogIn_IsSkipped()
        {
            SignedInUser user = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app);
            await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password);

            CleanupResult result = await RunAtAsync(_app.Now + DeletionRequests.GraceTime + TimeSpan.FromDays(10));

            Assert.Equal(0, result.AccountsPurged);
            Assert.Equal(user.Form.Email, await DeletionRows.UserValueAsync(_app, user.UserId, "email"));
        }

        [Fact]
        public async Task Post_GuestRequestedThirtyDaysAgo_IsPurged()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            await DeletionSteps.RequestAsync(guest.Client, DeletionRequests.KeepName);

            CleanupResult result = await RunAtAsync(_app.Now + DeletionRequests.GraceTime);

            Assert.Equal(1, result.AccountsPurged);
            Assert.NotNull((await DeletionRows.ReadAsync(_app, guest.UserId)).PurgedAt);
        }

        [Fact]
        public async Task Post_DueAccountWithKeepName_RemovesTheEmailAndKeepsTheName()
        {
            SignedInUser user = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app);

            await RunAtAsync(_app.Now + DeletionRequests.GraceTime);

            Assert.Null(await DeletionRows.UserValueAsync(_app, user.UserId, "email"));
            Assert.Equal(user.Form.DisplayName, await DeletionRows.UserValueAsync(_app, user.UserId, "display_name"));
        }

        [Fact]
        public async Task Post_DueAccountWithFormerMember_ShowsTheFormerMemberName()
        {
            SignedInUser user = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app, DeletionRequests.FormerMember);

            await RunAtAsync(_app.Now + DeletionRequests.GraceTime);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.UserValueAsync(_app, user.UserId, "display_name"));
        }

        [Fact]
        public async Task Post_AccountAlreadyPurgedByAnEarlierRun_IsNotCountedAgainAndKeepsItsFirstPurgeTime()
        {
            SignedInUser user = await CleanupSteps.CreateAccountRequestingDeletionAsync(_app);
            DateTimeOffset firstRun = _app.Now + DeletionRequests.GraceTime;
            await RunAtAsync(firstRun);

            CleanupResult second = await RunAtAsync(firstRun + TimeSpan.FromDays(5));

            Assert.Equal(0, second.AccountsPurged);
            Assert.Equal(firstRun, (await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        private async Task<CleanupResult> RunAtAsync(DateTimeOffset now)
        {
            _app.Clock.SetTo(now);

            return await _app.RunCleanupAsync();
        }
    }
}
