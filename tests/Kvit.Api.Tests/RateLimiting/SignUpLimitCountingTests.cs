using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Proxy;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.RateLimiting
{
    public class SignUpLimitCountingTests(ProxiedApp _app) : IClassFixture<ProxiedApp>
    {
        private const int SignUpsAllowed = 5;
        private const int RejectedAttempts = 30;

        [Theory]
        [InlineData(ResultCodes.AUTH_PASSWORD_TOO_WEAK)]
        [InlineData(ResultCodes.AUTH_EMAIL_INVALID)]
        [InlineData(ResultCodes.AUTH_DISPLAY_NAME_INVALID)]
        [InlineData(ResultCodes.TIME_ZONE_INVALID)]
        [InlineData(ResultCodes.LANGUAGE_INVALID)]
        public async Task SignUpsFailingTheCheapChecks_AreNeverCounted_SoAValidSignUpStillSucceeds(string errorCode)
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());

            for (int attempt = 1; attempt <= RejectedAttempts; attempt++)
            {
                HttpResponseMessage rejected = await RateLimitRequests.PostRegistrationAsync(client, InvalidFormFor(errorCode), null);

                await ProblemResponse.AssertAsync(rejected, HttpStatusCode.BadRequest, errorCode);
            }

            HttpResponseMessage valid = await RateLimitRequests.PostRegistrationAsync(client, RegistrationForm.Valid(), null);
            Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        }

        [Fact]
        public async Task FourWeakPasswordsThenFiveValidSignUps_SixthValidSignUpAnswers429RateLimitedWithRetryAfter()
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                await RateLimitRequests.PostRegistrationAsync(client, InvalidFormFor(ResultCodes.AUTH_PASSWORD_TOO_WEAK), null);
            }
            await AssertFiveValidSignUpsSucceedAsync(client);

            HttpResponseMessage refused = await RateLimitRequests.PostRegistrationAsync(client, RegistrationForm.Valid(), null);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.TooManyRequests, ResultCodes.RATE_LIMITED);
            RateLimitRequests.AssertRetryAfterInWholeSeconds(refused);
        }

        [Fact]
        public async Task FiveSignUpsWithATakenEmail_AreAnsweredEmailTakenAndCounted_SoTheSixthAnswers429RateLimited()
        {
            RegistrationForm existing = RegistrationForm.Valid();
            await _app.CreateAccountAsync(existing);
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            for (int attempt = 1; attempt <= SignUpsAllowed; attempt++)
            {
                HttpResponseMessage taken = await RateLimitRequests.PostRegistrationAsync(client, existing, null);

                await ProblemResponse.AssertAsync(taken, HttpStatusCode.BadRequest, ResultCodes.AUTH_EMAIL_TAKEN);
            }

            HttpResponseMessage refused = await RateLimitRequests.PostRegistrationAsync(client, existing, null);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.TooManyRequests, ResultCodes.RATE_LIMITED);
        }

        [Fact]
        public async Task AnotherAddress_CanStillSignUpWhileTheFirstAddressIsRefused()
        {
            HttpClient refusedVisitor = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            HttpClient otherVisitor = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            await AssertFiveValidSignUpsSucceedAsync(refusedVisitor);
            HttpResponseMessage refused = await RateLimitRequests.PostRegistrationAsync(refusedVisitor, RegistrationForm.Valid(), null);
            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);

            HttpResponseMessage answer = await RateLimitRequests.PostRegistrationAsync(otherVisitor, RegistrationForm.Valid(), null);

            Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        }

        [Fact]
        public async Task ClientSentXForwardedFor_DoesNotChangeTheAddressCountedForSignUps()
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            for (int attempt = 1; attempt <= SignUpsAllowed; attempt++)
            {
                await RateLimitRequests.PostRegistrationAsync(client, RegistrationForm.Valid(), $"198.51.100.{attempt}");
            }

            HttpResponseMessage refused = await RateLimitRequests.PostRegistrationAsync(client, RegistrationForm.Valid(), "198.51.100.200");

            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        }

        private static async Task AssertFiveValidSignUpsSucceedAsync(HttpClient client)
        {
            for (int attempt = 1; attempt <= SignUpsAllowed; attempt++)
            {
                HttpResponseMessage response = await RateLimitRequests.PostRegistrationAsync(client, RegistrationForm.Valid(), null);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private static RegistrationForm InvalidFormFor(string errorCode)
        {
            RegistrationForm valid = RegistrationForm.Valid();

            return errorCode switch
            {
                ResultCodes.AUTH_PASSWORD_TOO_WEAK => valid with { Password = "sunce2026" },
                ResultCodes.AUTH_EMAIL_INVALID => valid with { Email = "not-an-email" },
                ResultCodes.AUTH_DISPLAY_NAME_INVALID => valid with { DisplayName = "   " },
                ResultCodes.TIME_ZONE_INVALID => valid with { TimeZone = "Mars/Olympus" },
                ResultCodes.LANGUAGE_INVALID => valid with { Language = "de" },
                _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, "No invalid registration form is defined for this error code."),
            };
        }
    }
}
