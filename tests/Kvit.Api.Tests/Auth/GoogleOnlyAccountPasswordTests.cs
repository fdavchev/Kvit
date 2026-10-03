using System.Net;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class GoogleOnlyAccountPasswordTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        [Theory]
        [InlineData(AuthRequests.WrongPassword)]
        [InlineData(RegistrationForm.ValidPassword)]
        public async Task LogIn_AnyPassword_Answers400UsesGoogleAndSetsNoCookie(string password)
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.LogInAsync(client, identity.Email, password);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_USES_GOOGLE);
            Assert.Null(AuthCookie.Find(response));
        }

        [Fact]
        public async Task LogIn_SixTriesOnAGoogleOnlyAccount_NeverCountAsFailuresAndNeverLockTheAccount()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            HttpClient client = _app.CreateClient();

            for (int attempt = 1; attempt <= 6; attempt++)
            {
                HttpResponseMessage response = await AuthRequests.LogInAsync(client, identity.Email, AuthRequests.WrongPassword);

                await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_USES_GOOGLE);
            }

            AppUser user = await _app.FindUserAsync(identity.Email);
            Assert.Equal(0, user.LockoutCount);
            Assert.Equal(0, user.AccessFailedCount);
            Assert.Null(user.LockoutEnd);
        }

        [Fact]
        public async Task ChangePassword_SignedInAsAGoogleOnlyAccount_Answers400UsesGoogle()
        {
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(GoogleIdentities.Verified());

            HttpResponseMessage response = await AuthRequests.ChangePasswordAsync(client, AuthRequests.WrongPassword, RegistrationForm.ValidPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_USES_GOOGLE);
        }
    }
}
