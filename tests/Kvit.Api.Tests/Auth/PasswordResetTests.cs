using System.Net;
using System.Text.Json;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class PasswordResetTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const int MinimumTemporaryPasswordLength = 12;

        [Fact]
        public async Task Reset_KnownEmail_ReturnsATemporaryPasswordThatFollowsThePasswordRule()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);

            string temporaryPassword = await _app.ResetExistingAccountAsync(form.Email);

            Assert.True(temporaryPassword.Length >= MinimumTemporaryPasswordLength, $"The temporary password has {temporaryPassword.Length} characters, expected at least {MinimumTemporaryPasswordLength}.");
            Assert.True(AccountRules.ValidatePassword(temporaryPassword).IsSuccess, "The temporary password does not pass AccountRules.ValidatePassword.");
        }

        [Fact]
        public async Task Reset_TwoResets_ReturnDifferentPasswords()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);

            string first = await _app.ResetExistingAccountAsync(form.Email);
            string second = await _app.ResetExistingAccountAsync(form.Email);

            Assert.NotEqual(first, second);
        }

        [Fact]
        public async Task Reset_UnknownEmail_ReturnsNullAndChangesNothing()
        {
            RegistrationForm bystander = RegistrationForm.Valid();
            await _app.CreateAccountAsync(bystander);
            string? bystanderHashBefore = (await _app.FindUserAsync(bystander.Email)).PasswordHash;
            int usersBefore = await _app.CountUsersAsync();

            string? temporaryPassword = await _app.ResetPasswordAsync(RegistrationForm.UniqueEmail());

            Assert.Null(temporaryPassword);
            Assert.Equal(usersBefore, await _app.CountUsersAsync());
            Assert.Equal(bystanderHashBefore, (await _app.FindUserAsync(bystander.Email)).PasswordHash);
            Assert.False(await _app.MustChangePasswordAsync(bystander.Email));
        }

        [Fact]
        public async Task Reset_OldPassword_NoLongerLogsIn()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, account.Form.Email, account.Form.Password);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
        }

        [Fact]
        public async Task Reset_TemporaryPassword_LogsInAndAnswersMustChangePasswordTrue()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, account.Form.Email, account.TemporaryPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.True(body.GetProperty("mustChangePassword").GetBoolean());
        }

        [Fact]
        public async Task Reset_LockedAccount_IsUnlockedAtOnce()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpResponseMessage locking = await AuthRequests.LockByWrongLogInsAsync(_app.CreateClient(), form.Email);
            Assert.Equal(HttpStatusCode.Forbidden, locking.StatusCode);

            string temporaryPassword = await _app.ResetExistingAccountAsync(form.Email);

            AppUser unlocked = await _app.FindUserAsync(form.Email);
            Assert.Equal(0, unlocked.LockoutCount);
            Assert.Equal(0, unlocked.AccessFailedCount);
            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), form.Email, temporaryPassword);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Reset_SetsTheMustChangePasswordFlag()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            Assert.False(await _app.MustChangePasswordAsync(form.Email));

            await _app.ResetExistingAccountAsync(form.Email);

            Assert.True(await _app.MustChangePasswordAsync(form.Email));
        }

        [Fact]
        public async Task Reset_StoresOnlyAHashOfTheTemporaryPassword()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);

            string temporaryPassword = await _app.ResetExistingAccountAsync(form.Email);

            string storedHash = (await _app.FindUserAsync(form.Email)).PasswordHash ?? throw new InvalidOperationException($"The account '{form.Email}' has no stored password hash after a reset.");
            Assert.NotEqual(temporaryPassword, storedHash);
            Assert.DoesNotContain(temporaryPassword, storedHash);
        }

        [Fact]
        public async Task Reset_SessionFromBeforeTheReset_Answers401()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(client)).StatusCode);

            await _app.ResetExistingAccountAsync(form.Email);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
