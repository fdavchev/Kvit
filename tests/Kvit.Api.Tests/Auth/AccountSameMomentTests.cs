using System.Net;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class AccountSameMomentTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        private const string NewPassword = "Novo-lozinka7";
        private const string PasswordChanges = "OLD.password_hash IS DISTINCT FROM NEW.password_hash";

        [Fact]
        public async Task SetPassword_TwoAtTheSameMoment_TheFirstAnswers204AndTheSecondFindsThePasswordAlreadySet()
        {
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(GoogleIdentities.Verified());
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", "OLD.password_hash IS NULL AND NEW.password_hash IS NOT NULL");

            Task<HttpResponseMessage> first = AuthRequests.SetPasswordAsync(client, NewPassword);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = AuthRequests.SetPasswordAsync(client, "Drugo-lozinka8");
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal(HttpStatusCode.NoContent, responses[0].StatusCode);
            await ProblemResponse.AssertAsync(responses[1], HttpStatusCode.BadRequest, ResultCodes.AUTH_PASSWORD_ALREADY_SET);
        }

        [Fact]
        public async Task RequestDeletion_WhileAPasswordChangeOfTheSameAccountIsSaving_BothAnswer204AndTheDeletionIsStored()
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", PasswordChanges);

            Task<HttpResponseMessage> change = AuthRequests.ChangePasswordAsync(user.Client, user.Form.Password, NewPassword);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> deletion = DeletionRequests.DeleteAsync(user.Client, DeletionRequests.KeepName);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(change, deletion);

            Assert.Equal(HttpStatusCode.NoContent, responses[0].StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, responses[1].StatusCode);
            Assert.Equal(DeletionRequests.KeepName, (await DeletionRows.ReadAsync(_app, user.UserId)).NameChoice);
        }

        [Fact]
        public async Task ResetPasswordScript_WhileThePersonChangesTheirPassword_RunsOnceMoreAndLeavesATemporaryPasswordThatWorks()
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", $"{PasswordChanges} AND NOT NEW.must_change_password");

            Task<HttpResponseMessage> change = AuthRequests.ChangePasswordAsync(user.Client, user.Form.Password, NewPassword);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<string?> reset = _app.ResetPasswordAsync(user.Form.Email);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage changed = await change;
            string? temporaryPassword = await reset;

            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
            Assert.NotNull(temporaryPassword);
            Assert.True(await _app.MustChangePasswordAsync(user.Form.Email));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, temporaryPassword)).StatusCode);
        }

        [Fact]
        public async Task GrantAdminScript_WhileThePersonChangesTheirPassword_RunsOnceMoreAndGivesTheRole()
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", PasswordChanges);

            Task<HttpResponseMessage> change = AuthRequests.ChangePasswordAsync(user.Client, user.Form.Password, NewPassword);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<bool> grant = _app.GrantAdminAsync(user.Form.Email);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage changed = await change;
            bool granted = await grant;

            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
            Assert.True(granted);
            Assert.Equal(1, await _app.CountAdminRowsOfAsync(user.Form.Email));
        }

        [Fact]
        public async Task RevokeAdminScript_WhileThePersonChangesTheirPassword_RunsOnceMoreAndTakesTheRole()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.MakeAdminAsync(user.Form.Email);
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", PasswordChanges);

            Task<HttpResponseMessage> change = AuthRequests.ChangePasswordAsync(user.Client, user.Form.Password, NewPassword);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<bool> revoke = _app.RevokeAdminAsync(user.Form.Email);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage changed = await change;
            bool revoked = await revoke;

            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
            Assert.True(revoked);
            Assert.Equal(0, await _app.CountAdminRowsOfAsync(user.Form.Email));
        }
    }
}
