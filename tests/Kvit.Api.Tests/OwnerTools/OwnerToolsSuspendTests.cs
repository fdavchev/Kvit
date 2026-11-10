using System.Net;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsSuspendTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        [Fact]
        public async Task Suspend_Person_Answers204AndSetsSuspendedAtToTheClockNow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();

            HttpResponseMessage response = await OwnerToolsRequests.SuspendAsync(admin.Client, person.UserId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(_app.Clock.GetUtcNow(), await OwnerToolsRows.SuspendedAtAsync(_app, person.UserId));
        }

        [Fact]
        public async Task Suspend_Person_WritesOneLogRowWithTheAdminTheNameTheTrimmedReasonAndTheTime()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Dragan Ilievski");

            await OwnerToolsSteps.SuspendAsync(admin, person.UserId, "  Reported twice  ");

            StoredAction row = Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId));
            Assert.Equal(new StoredAction(admin.UserId, "Suspended", person.UserId, null, "Dragan Ilievski", "Reported twice", _app.Clock.GetUtcNow()), row);
        }

        [Fact]
        public async Task Suspend_PersonAlreadySuspended_Answers204KeepsTheFirstTimeAndWritesNoSecondRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            DateTimeOffset firstTime = _app.Clock.GetUtcNow();
            _app.Clock.Advance(TimeSpan.FromMinutes(5));

            HttpResponseMessage response = await OwnerToolsRequests.SuspendAsync(admin.Client, person.UserId, "A second reason");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(firstTime, await OwnerToolsRows.SuspendedAtAsync(_app, person.UserId));
            Assert.Equal(OwnerToolsRequests.DefaultReason, Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId)).Reason);
        }

        [Fact]
        public async Task Unsuspend_SuspendedPerson_Answers204ClearsTheTimeAndWritesOneRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Dragan Ilievski");
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            _app.Clock.Advance(TimeSpan.FromMinutes(5));

            HttpResponseMessage response = await OwnerToolsRequests.UnsuspendAsync(admin.Client, person.UserId, " Resolved ");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await OwnerToolsRows.SuspendedAtAsync(_app, person.UserId));
            List<StoredAction> rows = await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId);
            Assert.Equal(2, rows.Count);
            Assert.Equal(new StoredAction(admin.UserId, "Unsuspended", person.UserId, null, "Dragan Ilievski", "Resolved", _app.Clock.GetUtcNow()), rows[1]);
        }

        [Fact]
        public async Task Unsuspend_PersonWhoIsNotSuspended_Answers204AndWritesNoRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();

            HttpResponseMessage response = await OwnerToolsRequests.UnsuspendAsync(admin.Client, person.UserId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await OwnerToolsRows.SuspendedAtAsync(_app, person.UserId));
            Assert.Empty(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId));
        }

        [Fact]
        public async Task Unsuspend_TwiceInARow_WritesOnlyOneUnsuspendedRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            await OwnerToolsSteps.UnsuspendAsync(admin, person.UserId);

            HttpResponseMessage response = await OwnerToolsRequests.UnsuspendAsync(admin.Client, person.UserId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            List<StoredAction> rows = await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId);
            Assert.Equal(["Suspended", "Unsuspended"], rows.Select(row => row.Action));
        }

        [Fact]
        public async Task SuspendUnsuspendSuspend_WritesThreeRowsInOrder()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();

            await OwnerToolsSteps.SuspendAsync(admin, person.UserId, "first");
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.UnsuspendAsync(admin, person.UserId, "second");
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId, "third");

            List<StoredAction> rows = await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId);
            Assert.Equal(["Suspended", "Unsuspended", "Suspended"], rows.Select(row => row.Action));
            Assert.Equal(["first", "second", "third"], rows.Select(row => row.Reason));
            Assert.NotNull(await OwnerToolsRows.SuspendedAtAsync(_app, person.UserId));
        }

        [Theory]
        [MemberData(nameof(OwnerToolsCalls.PersonNames), MemberType = typeof(OwnerToolsCalls))]
        public async Task Action_OnAnotherAdmin_Answers403CannotActOnAdminAndChangesNothing(string name)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser otherAdmin = await _app.SignInAdminAsync();
            string before = await DeletionRows.SnapshotOfUserRowAsync(_app, otherAdmin.UserId);
            int logRowsBefore = await OwnerToolsRows.CountActionsAsync(_app);

            HttpResponseMessage response = await call.SendAsync(admin.Client, otherAdmin.UserId, Guid.NewGuid(), OwnerToolsRequests.DefaultReason);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.CannotActOnAdminCode);
            Assert.Equal(before, await DeletionRows.SnapshotOfUserRowAsync(_app, otherAdmin.UserId));
            Assert.Equal(logRowsBefore, await OwnerToolsRows.CountActionsAsync(_app));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(otherAdmin.Client)).StatusCode);
        }

        [Theory]
        [MemberData(nameof(OwnerToolsCalls.PersonNames), MemberType = typeof(OwnerToolsCalls))]
        public async Task Action_OnTheCallerThemselves_Answers403CannotActOnAdminAndChangesNothing(string name)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            SignedInUser admin = await _app.SignInAdminAsync();
            string before = await DeletionRows.SnapshotOfUserRowAsync(_app, admin.UserId);
            int logRowsBefore = await OwnerToolsRows.CountActionsAsync(_app);

            HttpResponseMessage response = await call.SendAsync(admin.Client, admin.UserId, Guid.NewGuid(), OwnerToolsRequests.DefaultReason);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.CannotActOnAdminCode);
            Assert.Equal(before, await DeletionRows.SnapshotOfUserRowAsync(_app, admin.UserId));
            Assert.Equal(logRowsBefore, await OwnerToolsRows.CountActionsAsync(_app));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(admin.Client)).StatusCode);
        }

        [Theory]
        [MemberData(nameof(OwnerToolsCalls.PersonNames), MemberType = typeof(OwnerToolsCalls))]
        public async Task Action_OnAPersonWhoDoesNotExist_Answers404PersonNotFoundAndWritesNoRow(string name)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            SignedInUser admin = await _app.SharedAdminAsync();
            int logRowsBefore = await OwnerToolsRows.CountActionsAsync(_app);

            HttpResponseMessage response = await call.SendAsync(admin.Client, Guid.NewGuid(), Guid.NewGuid(), OwnerToolsRequests.DefaultReason);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, OwnerToolsRequests.PersonNotFoundCode);
            Assert.Equal(logRowsBefore, await OwnerToolsRows.CountActionsAsync(_app));
        }

        [Fact]
        public async Task Suspend_Person_KeepsTheirSecurityStampNameEmailPasswordAndLanguage()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            string stampBefore = await DeletionRows.UserValueAsync(_app, person.UserId, "security_stamp") ?? string.Empty;
            string rowBefore = await DeletionRows.UserValueAsync(_app, person.UserId, "(display_name || '|' || email || '|' || password_hash || '|' || language)") ?? string.Empty;

            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);

            Assert.Equal(stampBefore, await DeletionRows.UserValueAsync(_app, person.UserId, "security_stamp"));
            Assert.Equal(rowBefore, await DeletionRows.UserValueAsync(_app, person.UserId, "(display_name || '|' || email || '|' || password_hash || '|' || language)"));
            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, person.UserId));
        }

        [Fact]
        public async Task Suspend_Person_TouchesNoOtherPersonAndNoGroupRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string ownerBefore = await DeletionRows.SnapshotOfUserRowAsync(_app, setup.Owner.UserId);
            string groupBefore = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            await OwnerToolsSteps.SuspendAsync(admin, setup.Member.UserId);

            Assert.Equal(ownerBefore, await DeletionRows.SnapshotOfUserRowAsync(_app, setup.Owner.UserId));
            Assert.Equal(groupBefore, await GroupRows.SnapshotAsync(_app, setup.GroupId));
            Assert.Null(await OwnerToolsRows.SuspendedAtAsync(_app, setup.Owner.UserId));
            Assert.Empty(await OwnerToolsRows.ActionsAboutAsync(_app, setup.Owner.UserId));
        }

        [Fact]
        public async Task Suspend_PersonWhoIsAGuest_Answers204AndSetsSuspendedAt()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, setup.GroupId, "Baba Mara");

            HttpResponseMessage response = await OwnerToolsRequests.SuspendAsync(admin.Client, guest.UserId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.NotNull(await OwnerToolsRows.SuspendedAtAsync(_app, guest.UserId));
            Assert.Equal("Baba Mara", Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, guest.UserId)).TargetLabel);
        }

        [Fact]
        public async Task Suspend_ByASecondAdmin_LogsThatAdminAsTheActor()
        {
            SignedInUser secondAdmin = await _app.SignInAdminAsync("Second admin");
            SignedInUser person = await _app.SignUpAsync();

            await OwnerToolsSteps.SuspendAsync(secondAdmin, person.UserId);

            Assert.Equal(secondAdmin.UserId, Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId)).AdminUserId);
        }

        [Fact]
        public async Task Suspend_PersonWhoWasPurgedKeepingTheirName_WritesFormerMemberNotTheName()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            await DeletionSteps.RequestAndPurgeAsync(_app, person.Client, person.UserId, DeletionRequests.KeepName);

            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId)).TargetLabel);
        }
    }
}
