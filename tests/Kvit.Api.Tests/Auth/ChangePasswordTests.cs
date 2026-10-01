using System.Net;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class ChangePasswordTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const string NewPassword = "Novo-lozinka7";

        [Fact]
        public async Task ChangePassword_RightCurrentPassword_Answers204WithNoBody()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);

            HttpResponseMessage response = await AuthRequests.ChangePasswordAsync(client, form.Password, NewPassword);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ChangePassword_ThenLogInWithTheNewPassword_Answers200()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            await AuthRequests.ChangePasswordAsync(client, form.Password, NewPassword);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), form.Email, NewPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_ThenLogInWithTheOldPassword_Answers401InvalidCredentials()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            await AuthRequests.ChangePasswordAsync(client, form.Password, NewPassword);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), form.Email, form.Password);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
        }

        [Fact]
        public async Task ChangePassword_AfterATemporaryPassword_ClearsTheMustChangePasswordFlag()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);
            Assert.True(await _app.MustChangePasswordAsync(account.Form.Email));

            await AuthRequests.ChangePasswordAsync(client, account.TemporaryPassword, NewPassword);

            Assert.False(await _app.MustChangePasswordAsync(account.Form.Email));
        }

        [Fact]
        public async Task ChangePassword_ForANormalAccount_KeepsTheMustChangePasswordFlagFalse()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);

            await AuthRequests.ChangePasswordAsync(client, form.Password, NewPassword);

            Assert.False(await _app.MustChangePasswordAsync(form.Email));
        }

        [Fact]
        public async Task ChangePassword_WithATemporaryPassword_IsAllowedAndAnswers204()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);

            HttpResponseMessage response = await AuthRequests.ChangePasswordAsync(client, account.TemporaryPassword, NewPassword);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_ThisSession_StaysSignedIn()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            HttpResponseMessage changed = await AuthRequests.ChangePasswordAsync(client, form.Password, NewPassword);
            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_AnotherSessionOfTheSameAccount_Answers401OnItsNextRequest()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient changingClient = await _app.CreateRegisteredClientAsync(form);
            HttpClient otherClient = await _app.CreateLoggedInClientAsync(form.Email, form.Password);
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(otherClient)).StatusCode);
            await AuthRequests.ChangePasswordAsync(changingClient, form.Password, NewPassword);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(otherClient);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.ChangePasswordAsync(client, RegistrationForm.ValidPassword, NewPassword);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_Answers400CurrentPasswordWrong()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);

            HttpResponseMessage response = await AuthRequests.ChangePasswordAsync(client, AuthRequests.WrongPassword, NewPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_CURRENT_PASSWORD_WRONG);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_KeepsTheStoredPassword()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            string? hashBefore = (await _app.FindUserAsync(form.Email)).PasswordHash;

            await AuthRequests.ChangePasswordAsync(client, AuthRequests.WrongPassword, NewPassword);

            Assert.Equal(hashBefore, (await _app.FindUserAsync(form.Email)).PasswordHash);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_CountsAsAFailedTry()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);

            await AuthRequests.ChangePasswordAsync(client, AuthRequests.WrongPassword, NewPassword);

            Assert.Equal(1, (await _app.FindUserAsync(form.Email)).AccessFailedCount);
        }

        [Fact]
        public async Task ChangePassword_FifthWrongCurrentPassword_Answers403LockedOutAndLocksTheAccount()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);

            HttpResponseMessage fifth = await LockByWrongCurrentPasswordsAsync(client);

            await ProblemResponse.AssertAsync(fifth, HttpStatusCode.Forbidden, ResultCodes.AUTH_LOCKED_OUT);
            Assert.Equal(1, (await _app.FindUserAsync(form.Email)).LockoutCount);
            HttpResponseMessage logIn = await AuthRequests.LogInAsync(_app.CreateClient(), form.Email, form.Password);
            await ProblemResponse.AssertAsync(logIn, HttpStatusCode.Forbidden, ResultCodes.AUTH_LOCKED_OUT);
        }

        [Fact]
        public async Task ChangePassword_RightCurrentPasswordWhileLocked_Answers403AndChangesNothing()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            await LockByWrongCurrentPasswordsAsync(client);
            string? hashBefore = (await _app.FindUserAsync(form.Email)).PasswordHash;

            HttpResponseMessage response = await AuthRequests.ChangePasswordAsync(client, form.Password, NewPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.AUTH_LOCKED_OUT);
            Assert.Equal(hashBefore, (await _app.FindUserAsync(form.Email)).PasswordHash);
        }

        [Theory]
        [InlineData("Sh0rt")]
        [InlineData("nouppercase1")]
        [InlineData("NoDigitsHere")]
        public async Task ChangePassword_NewPasswordBreaksTheRule_Answers400TooWeakAndKeepsThePasswordAndTheFlag(string weakPassword)
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);
            string? hashBefore = (await _app.FindUserAsync(account.Form.Email)).PasswordHash;

            HttpResponseMessage response = await AuthRequests.ChangePasswordAsync(client, account.TemporaryPassword, weakPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_PASSWORD_TOO_WEAK);
            Assert.Equal(hashBefore, (await _app.FindUserAsync(account.Form.Email)).PasswordHash);
            Assert.True(await _app.MustChangePasswordAsync(account.Form.Email));
        }

        [Fact]
        public async Task ChangePassword_NewPasswordEqualToTheCurrentOne_Answers400PasswordUnchanged()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            string? hashBefore = (await _app.FindUserAsync(form.Email)).PasswordHash;

            HttpResponseMessage response = await AuthRequests.ChangePasswordAsync(client, form.Password, form.Password);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_PASSWORD_UNCHANGED);
            Assert.Equal(hashBefore, (await _app.FindUserAsync(form.Email)).PasswordHash);
        }

        [Fact]
        public async Task ChangePassword_TemporaryPasswordChangedToItself_Answers400PasswordUnchangedAndKeepsTheFlag()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);

            HttpResponseMessage response = await AuthRequests.ChangePasswordAsync(client, account.TemporaryPassword, account.TemporaryPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_PASSWORD_UNCHANGED);
            Assert.True(await _app.MustChangePasswordAsync(account.Form.Email));
        }

        private static async Task<HttpResponseMessage> LockByWrongCurrentPasswordsAsync(HttpClient client)
        {
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                HttpResponseMessage response = await AuthRequests.ChangePasswordAsync(client, AuthRequests.WrongPassword, NewPassword);

                await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_CURRENT_PASSWORD_WRONG);
            }

            return await AuthRequests.ChangePasswordAsync(client, AuthRequests.WrongPassword, NewPassword);
        }
    }
}
