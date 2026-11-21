using System.Net;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Contracts.Cleanup;
using Kvit.Domain.Cleanup;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupGuestTests(CleanupApp app) : CleanupTestBase(app)
    {
        private static readonly TimeSpan LeftBehindTime = TimeSpan.FromDays(CleanupRules.LeftBehindGuestDays);

        [Fact]
        public async Task Run_GuestLetInAgainAndCreatedThirtyDaysAgo_IsPurgedAndKeepsTheNameInTheHistory()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            DateTimeOffset createdAt = _app.Now;
            await LetInAgainAsync(group, guest);

            CleanupResponse response = await RunAtAsync(createdAt + LeftBehindTime);

            Assert.True(response.GuestsPurged >= 1);
            Assert.Equal(0, response.AccountsPurged);
            Assert.Equal(createdAt + LeftBehindTime, (await DeletionRows.ReadAsync(_app, guest.UserId)).PurgedAt);
            Assert.Equal("Marko", await DeletionRows.UserValueAsync(_app, guest.UserId, "display_name"));
        }

        [Fact]
        public async Task Run_GuestLetInAgainButCreatedLessThanThirtyDaysAgo_IsKept()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            DateTimeOffset createdAt = _app.Now;
            await LetInAgainAsync(group, guest);

            await RunAtAsync(createdAt + LeftBehindTime - CleanupSteps.OneSecond);

            Assert.Null((await DeletionRows.ReadAsync(_app, guest.UserId)).PurgedAt);
        }

        [Fact]
        public async Task Run_OldGuestStillInAGroup_IsKept()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            await RunAtAsync(_app.Now + LeftBehindTime + TimeSpan.FromDays(365));

            Assert.Null((await DeletionRows.ReadAsync(_app, guest.UserId)).PurgedAt);
        }

        [Fact]
        public async Task Run_OldGuestWhoLeftTheGroup_IsKeptBecauseTheEndedRowIsStillTheirs()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            Assert.Equal(HttpStatusCode.NoContent, (await GroupRequests.LeaveAsync(guest.Client, group.GroupId)).StatusCode);

            await RunAtAsync(_app.Now + LeftBehindTime + TimeSpan.FromDays(365));

            Assert.Null((await DeletionRows.ReadAsync(_app, guest.UserId)).PurgedAt);
        }

        [Fact]
        public async Task Run_OldRealAccountWithoutAnyGroup_IsNeverPurged()
        {
            SignedInUser user = await _app.SignUpAsync();

            await RunAtAsync(_app.Now + LeftBehindTime + TimeSpan.FromDays(365));

            Assert.Null((await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
            Assert.Equal(user.Form.Email, await DeletionRows.UserValueAsync(_app, user.UserId, "email"));
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
