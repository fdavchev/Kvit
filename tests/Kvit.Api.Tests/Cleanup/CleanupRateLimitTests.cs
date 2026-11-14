using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Proxy;
using Kvit.Api.Tests.RateLimiting;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupRateLimitTests(CleanupProxiedApp _app) : IClassFixture<CleanupProxiedApp>
    {
        private const int Allowed = 5;

        [Fact]
        public async Task Post_UpToTheAllowedNumberFromOneAddress_AllAnswer200()
        {
            HttpClient client = NewVisitor();

            for (int call = 1; call <= Allowed; call++)
            {
                HttpResponseMessage response = await CleanupRequests.PostAsync(client, CleanupApp.Secret);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task Post_AfterTheAllowedNumberFromOneAddress_Answers429RateLimitedWithRetryAfter()
        {
            HttpClient client = NewVisitor();
            await PostManyAsync(client, Allowed);

            HttpResponseMessage refused = await CleanupRequests.PostAsync(client, CleanupApp.Secret);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.TooManyRequests, ResultCodes.RATE_LIMITED);
            RateLimitRequests.AssertRetryAfterInWholeSeconds(refused);
        }

        [Fact]
        public async Task Post_AnotherAddress_IsStillAllowedWhileOneAddressIsRefused()
        {
            HttpClient refusedVisitor = NewVisitor();
            await PostManyAsync(refusedVisitor, Allowed);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await CleanupRequests.PostAsync(refusedVisitor, CleanupApp.Secret)).StatusCode);

            HttpResponseMessage response = await CleanupRequests.PostAsync(NewVisitor(), CleanupApp.Secret);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Post_WrongSecretsAreCountedToo_SixthWrongAttemptAnswers429()
        {
            HttpClient client = NewVisitor();
            for (int call = 1; call <= Allowed; call++)
            {
                HttpResponseMessage refused = await CleanupRequests.PostAsync(client, "wrong-secret");

                Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
            }

            HttpResponseMessage limited = await CleanupRequests.PostAsync(client, "wrong-secret");

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        }

        [Fact]
        public async Task Post_RightSecretAfterFiveWrongOnes_Answers429()
        {
            HttpClient client = NewVisitor();
            for (int call = 1; call <= Allowed; call++)
            {
                await CleanupRequests.PostAsync(client, "wrong-secret");
            }

            HttpResponseMessage limited = await CleanupRequests.PostAsync(client, CleanupApp.Secret);

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        }

        [Theory]
        [InlineData("/health")]
        [InlineData("/api/health")]
        public async Task Health_AfterTheCleanupLimitIsUsedUp_StillAnswersHealthy(string path)
        {
            HttpClient client = NewVisitor();
            await PostManyAsync(client, Allowed + 1);

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Post_RightCleanupSecretWithoutTheProxySecret_Answers403()
        {
            HttpClient client = _app.CreateCleanupClientSending(null, _app.NewVisitorAddress());

            HttpResponseMessage response = await CleanupRequests.PostAsync(client, CleanupApp.Secret);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Post_RightCleanupSecretWithAWrongProxySecret_Answers403()
        {
            HttpClient client = _app.CreateCleanupClientSending("not-the-proxy-secret", _app.NewVisitorAddress());

            HttpResponseMessage response = await CleanupRequests.PostAsync(client, CleanupApp.Secret);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.Forbidden);
        }

        private HttpClient NewVisitor()
        {
            return _app.CreateCleanupClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
        }

        private static async Task PostManyAsync(HttpClient client, int count)
        {
            for (int call = 1; call <= count; call++)
            {
                await CleanupRequests.PostAsync(client, CleanupApp.Secret);
            }
        }
    }
}
