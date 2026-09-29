using System.Net;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class LockoutTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const string WrongPassword = "Wrong-password-1";

        [Fact]
        public async Task LogIn_FirstFourWrongPasswords_Answer401AndCountTheFailures()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            for (int attempt = 1; attempt <= 4; attempt++)
            {
                HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, WrongPassword);

                await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
            }

            AppUser user = await _app.FindUserAsync(form.Email);
            Assert.Equal(4, user.AccessFailedCount);
            Assert.Equal(0, user.LockoutCount);
            Assert.Null(user.LockoutEnd);
        }

        [Fact]
        public async Task LogIn_FifthWrongPassword_LocksTheAccountForAboutFiveMinutes()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage fifth = await LockAccountAsync(client, form.Email);

            await ProblemResponse.AssertAsync(fifth, HttpStatusCode.Forbidden, ResultCodes.AUTH_LOCKED_OUT);
            AppUser user = await _app.FindUserAsync(form.Email);
            Assert.Equal(1, user.LockoutCount);
            AssertLockEndsInMinutes(user, 4, 5);
        }

        [Fact]
        public async Task LogIn_RightPasswordWhileLocked_Answers403AndSetsNoCookie()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();
            await LockAccountAsync(client, form.Email);

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, form.Email, form.Password);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.AUTH_LOCKED_OUT);
            Assert.Null(AuthCookie.Find(response));
        }

        [Fact]
        public async Task LogIn_LockedAccount_DoesNotLockOtherAccounts()
        {
            RegistrationForm locked = RegistrationForm.Valid();
            RegistrationForm bystander = RegistrationForm.Valid();
            await _app.CreateAccountAsync(locked);
            await _app.CreateAccountAsync(bystander);
            HttpClient client = _app.CreateClient();
            await LockAccountAsync(client, locked.Email);

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, bystander.Email, bystander.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task LockLadder_GrowsFrom5To10To15MinutesAndStaysAt15()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();
            (int LockoutCount, int MinMinutes, int MaxMinutes)[] rungs = [(1, 4, 5), (2, 9, 10), (3, 14, 15), (4, 14, 15)];

            foreach ((int expectedLockoutCount, int minMinutes, int maxMinutes) in rungs)
            {
                HttpResponseMessage fifth = await LockAccountAsync(client, form.Email);

                await ProblemResponse.AssertAsync(fifth, HttpStatusCode.Forbidden, ResultCodes.AUTH_LOCKED_OUT);
                AppUser user = await _app.FindUserAsync(form.Email);
                Assert.Equal(expectedLockoutCount, user.LockoutCount);
                AssertLockEndsInMinutes(user, minMinutes, maxMinutes);
                await _app.EndLockAsync(form.Email);
            }
        }

        [Fact]
        public async Task LogIn_SuccessAfterALockEnded_ResetsTheLadder()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            HttpClient client = _app.CreateClient();
            await LockAccountAsync(client, form.Email);
            await _app.EndLockAsync(form.Email);
            HttpResponseMessage wrong = await AuthRequests.LogInAsync(client, form.Email, WrongPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

            HttpResponseMessage right = await AuthRequests.LogInAsync(client, form.Email, form.Password);

            Assert.Equal(HttpStatusCode.OK, right.StatusCode);
            AppUser afterSuccess = await _app.FindUserAsync(form.Email);
            Assert.Equal(0, afterSuccess.LockoutCount);
            Assert.Equal(0, afterSuccess.AccessFailedCount);
            HttpResponseMessage fifth = await LockAccountAsync(client, form.Email);
            await ProblemResponse.AssertAsync(fifth, HttpStatusCode.Forbidden, ResultCodes.AUTH_LOCKED_OUT);
            AppUser lockedAgain = await _app.FindUserAsync(form.Email);
            Assert.Equal(1, lockedAgain.LockoutCount);
            AssertLockEndsInMinutes(lockedAgain, 4, 5);
        }

        [Fact]
        public async Task LogIn_WrongPasswordsForAnUnknownEmail_AlwaysAnswer401AndChangeNothing()
        {
            RegistrationForm bystander = RegistrationForm.Valid();
            await _app.CreateAccountAsync(bystander);
            HttpClient client = _app.CreateClient();
            string unknownEmail = RegistrationForm.UniqueEmail();
            int usersBefore = await _app.CountUsersAsync();
            int eventsBefore = await _app.CountUsageEventsAsync();

            for (int attempt = 1; attempt <= 6; attempt++)
            {
                HttpResponseMessage response = await AuthRequests.LogInAsync(client, unknownEmail, WrongPassword);

                await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
            }

            Assert.Equal(usersBefore, await _app.CountUsersAsync());
            Assert.Equal(eventsBefore, await _app.CountUsageEventsAsync());
            Assert.Equal(0, await _app.CountUsersWithEmailAsync(unknownEmail));
            AppUser untouched = await _app.FindUserAsync(bystander.Email);
            Assert.Equal(0, untouched.AccessFailedCount);
            Assert.Equal(0, untouched.LockoutCount);
        }

        private static void AssertLockEndsInMinutes(AppUser user, int minMinutes, int maxMinutes)
        {
            DateTimeOffset lockEnd = user.LockoutEnd ?? throw new InvalidOperationException($"LockoutEnd is null for '{user.Email}', but the account should be locked.");

            Assert.InRange(lockEnd - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(minMinutes), TimeSpan.FromMinutes(maxMinutes));
        }

        private static async Task<HttpResponseMessage> LockAccountAsync(HttpClient client, string email)
        {
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                HttpResponseMessage response = await AuthRequests.LogInAsync(client, email, WrongPassword);

                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }

            return await AuthRequests.LogInAsync(client, email, WrongPassword);
        }
    }
}
