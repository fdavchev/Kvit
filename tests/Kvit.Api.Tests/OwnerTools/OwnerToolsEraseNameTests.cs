using System.Net;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsEraseNameTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        [Fact]
        public async Task EraseName_PersonWhoNeverAskedToDelete_Answers204AndPurgesTheAccountAtOnce()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");

            HttpResponseMessage response = await OwnerToolsRequests.EraseNameAsync(admin.Client, person.UserId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            StoredDeletion stored = await DeletionRows.ReadAsync(_app, person.UserId);
            Assert.Equal(_app.Clock.GetUtcNow(), stored.PurgedAt);
            Assert.Equal(DeletionRequests.FormerMember, stored.NameChoice);
        }

        [Fact]
        public async Task EraseName_Person_ReplacesTheNameWithFormerMemberAndClearsTheEmail()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");

            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.UserValueAsync(_app, person.UserId, "display_name"));
            Assert.Null(await DeletionRows.UserValueAsync(_app, person.UserId, "email"));
            Assert.Null(await DeletionRows.UserValueAsync(_app, person.UserId, "password_hash"));
        }

        [Fact]
        public async Task EraseName_PersonWhoUsesMacedonian_GetsTheMacedonianWord()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Зорица Петровска");
            await AuthRequests.ChangeLanguageAsync(person.Client, "mk");

            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);

            Assert.Equal(DeletionRequests.FormerMemberMacedonian, await DeletionRows.UserValueAsync(_app, person.UserId, "display_name"));
        }

        [Fact]
        public async Task EraseName_PersonWhoAskedToDeleteAndKeepTheirName_StillGetsFormerMember()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            await DeletionSteps.RequestAsync(person.Client, DeletionRequests.KeepName);

            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);

            StoredDeletion stored = await DeletionRows.ReadAsync(_app, person.UserId);
            Assert.NotNull(stored.PurgedAt);
            Assert.Equal(DeletionRequests.FormerMember, stored.NameChoice);
            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.UserValueAsync(_app, person.UserId, "display_name"));
        }

        [Fact]
        public async Task EraseName_Person_EndsTheirSessionsAtOnce()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(person.Client)).StatusCode);

            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            Assert.Equal(HttpStatusCode.Unauthorized, (await AuthRequests.GetMeAsync(person.Client)).StatusCode);
        }

        [Fact]
        public async Task EraseName_Person_CannotLogInAgain()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), person.Form.Email, person.Form.Password);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
        }

        [Fact]
        public async Task EraseName_Person_WritesOneLogRowWithFormerMemberInsteadOfTheErasedName()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");

            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId, "  Asked by email  ");

            StoredAction row = Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId));
            Assert.Equal(new StoredAction(admin.UserId, "NameErased", person.UserId, null, DeletionRequests.FormerMemberEnglish, "Asked by email", _app.Clock.GetUtcNow()), row);
        }

        [Fact]
        public async Task EraseName_PersonWhoUsesMacedonian_TheLogRowStillSaysFormerMemberInEnglish()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Зорица Петровска");
            await AuthRequests.ChangeLanguageAsync(person.Client, "mk");

            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId)).TargetLabel);
        }

        [Fact]
        public async Task EraseName_PersonWithEarlierSuspendRows_NoRowAboutThemShowsTheOldNameAnyMore()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            await OwnerToolsSteps.UnsuspendAsync(admin, person.UserId);

            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);

            List<StoredAction> rows = await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId);
            Assert.Equal(["Suspended", "Unsuspended", "NameErased"], rows.Select(row => row.Action));
            Assert.All(rows, row => Assert.Equal(DeletionRequests.FormerMemberEnglish, row.TargetLabel));
            HttpResponseMessage log = await OwnerToolsRequests.LogAsync(admin.Client);
            Assert.DoesNotContain("Zorica Petrovska", await log.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        }

        [Fact]
        public async Task EraseName_Person_LeavesLogRowsAboutOtherPeopleUnchanged()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            SignedInUser bystander = await _app.SignUpAsync("Dragan Ilievski");
            await OwnerToolsSteps.SuspendAsync(admin, bystander.UserId);

            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);

            Assert.Equal("Dragan Ilievski", Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, bystander.UserId)).TargetLabel);
        }

        [Fact]
        public async Task EraseName_WhenTheLogRowCannotBeSaved_Answers500AndThePurgeIsUndone()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            string userBefore = await DeletionRows.SnapshotOfUserRowAsync(_app, person.UserId);
            string tablesBefore = await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app);
            await _app.ExecuteAsync(
                "CREATE OR REPLACE FUNCTION block_name_erased_row() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.action = 'NameErased' THEN RAISE EXCEPTION 'log row is blocked by the test'; END IF; RETURN NEW; END $$; "
                + "DROP TRIGGER IF EXISTS block_name_erased_row ON admin_actions; "
                + "CREATE TRIGGER block_name_erased_row BEFORE INSERT ON admin_actions FOR EACH ROW EXECUTE FUNCTION block_name_erased_row();");

            HttpResponseMessage response;
            try
            {
                response = await OwnerToolsRequests.EraseNameAsync(admin.Client, person.UserId);
            }
            finally
            {
                await _app.ExecuteAsync("DROP TRIGGER IF EXISTS block_name_erased_row ON admin_actions");
            }

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("log row is blocked by the test", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
            Assert.Equal(userBefore, await DeletionRows.SnapshotOfUserRowAsync(_app, person.UserId));
            Assert.Equal(tablesBefore, await DeletionRows.SnapshotOfEveryTableExceptUsersAsync(_app));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(person.Client)).StatusCode);
        }

        [Fact]
        public async Task OwnDeletion_PurgedAfterThirtyDays_EarlierSuspendRowsNoLongerShowTheName()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);
            await OwnerToolsSteps.UnsuspendAsync(admin, person.UserId);
            DateTimeOffset requestedAt = _app.Clock.GetUtcNow();
            await DeletionSteps.RequestAsync(person.Client, DeletionRequests.KeepName);

            await AccountPurges.PurgeDueAsync(_app, requestedAt + DeletionRequests.GraceTime);

            Assert.NotNull((await DeletionRows.ReadAsync(_app, person.UserId)).PurgedAt);
            Assert.All(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId), row => Assert.Equal(DeletionRequests.FormerMemberEnglish, row.TargetLabel));
        }

        [Fact]
        public async Task EraseName_PersonWhoIsAlreadyErased_Answers204ChangesNothingAndWritesNoSecondRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);
            string before = await DeletionRows.SnapshotOfUserRowAsync(_app, person.UserId);
            _app.Clock.Advance(TimeSpan.FromMinutes(5));

            HttpResponseMessage response = await OwnerToolsRequests.EraseNameAsync(admin.Client, person.UserId, "Again");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await DeletionRows.SnapshotOfUserRowAsync(_app, person.UserId));
            Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId));
        }

        [Fact]
        public async Task EraseName_PersonWhoseOwnDeletionWasAlreadyPurged_Answers204ChangesNothingAndWritesNoRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            await DeletionSteps.RequestAndPurgeAsync(_app, person.Client, person.UserId, DeletionRequests.KeepName);
            string before = await DeletionRows.SnapshotOfUserRowAsync(_app, person.UserId);

            HttpResponseMessage response = await OwnerToolsRequests.EraseNameAsync(admin.Client, person.UserId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await DeletionRows.SnapshotOfUserRowAsync(_app, person.UserId));
            Assert.Equal("Zorica Petrovska", await DeletionRows.UserValueAsync(_app, person.UserId, "display_name"));
            Assert.Empty(await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId));
        }

        [Fact]
        public async Task EraseName_MemberOfAGroup_ShowsAsFormerMemberInThatGroupAndKeepsTheirRowId()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await UserRows.SetDisplayNameAsync(_app, group.Member.UserId, "Zorica Petrovska");

            await OwnerToolsSteps.EraseNameAsync(admin, group.Member.UserId);

            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "user_id"));
            Assert.Equal(DeletionRequests.FormerMemberEnglish, await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "name"));
            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(group.Owner.Client, group.GroupId)).StatusCode);
        }

        [Fact]
        public async Task EraseName_Person_TouchesNoOtherPersonsRow()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync();
            SignedInUser bystander = await _app.SignUpAsync();
            string bystanderBefore = await DeletionRows.SnapshotOfUserRowAsync(_app, bystander.UserId);

            await OwnerToolsSteps.EraseNameAsync(admin, person.UserId);

            Assert.Equal(bystanderBefore, await DeletionRows.SnapshotOfUserRowAsync(_app, bystander.UserId));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(bystander.Client)).StatusCode);
            Assert.Empty(await OwnerToolsRows.ActionsAboutAsync(_app, bystander.UserId));
        }

        [Fact]
        public async Task EraseName_SuspendedPerson_IsStillErased()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser person = await _app.SignUpAsync("Zorica Petrovska");
            await OwnerToolsSteps.SuspendAsync(admin, person.UserId);

            HttpResponseMessage response = await OwnerToolsRequests.EraseNameAsync(admin.Client, person.UserId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.NotNull((await DeletionRows.ReadAsync(_app, person.UserId)).PurgedAt);
            Assert.Equal(["Suspended", "NameErased"], (await OwnerToolsRows.ActionsAboutAsync(_app, person.UserId)).Select(row => row.Action));
        }
    }
}
