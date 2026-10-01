using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Proxy;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.RateLimiting
{
    public class RateLimitTests(ProxiedApp _app) : IClassFixture<ProxiedApp>
    {
        private const string LogInPath = "/api/auth/login";
        private const string ChangePasswordPath = "/api/auth/change-password";

        [Theory]
        [InlineData(LogInPath, 10)]
        [InlineData(ChangePasswordPath, 10)]
        [InlineData(AuthRequests.RegisterPath, 5)]
        public async Task Request_AfterTheAllowedNumberFromOneAddress_Answers429RateLimited(string path, int allowed)
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());

            await AssertNotRefusedAsync(client, path, allowed);
            HttpResponseMessage refused = await RateLimitRequests.PostCountedAttemptAsync(client, path, null);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.TooManyRequests, ResultCodes.RATE_LIMITED);
        }

        [Theory]
        [InlineData(LogInPath, 10)]
        [InlineData(ChangePasswordPath, 10)]
        [InlineData(AuthRequests.RegisterPath, 5)]
        public async Task RefusedRequest_CarriesARetryAfterInWholeSeconds(string path, int allowed)
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            await AssertNotRefusedAsync(client, path, allowed);

            HttpResponseMessage refused = await RateLimitRequests.PostCountedAttemptAsync(client, path, null);

            RateLimitRequests.AssertRetryAfterInWholeSeconds(refused);
        }

        [Theory]
        [InlineData(LogInPath, 10)]
        [InlineData(ChangePasswordPath, 10)]
        [InlineData(AuthRequests.RegisterPath, 5)]
        public async Task AnotherAddress_IsStillAllowedWhileOneAddressIsRefused(string path, int allowed)
        {
            HttpClient refusedVisitor = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            HttpClient otherVisitor = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            await AssertNotRefusedAsync(refusedVisitor, path, allowed);
            HttpResponseMessage refused = await RateLimitRequests.PostCountedAttemptAsync(refusedVisitor, path, null);
            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);

            HttpResponseMessage answer = await RateLimitRequests.PostCountedAttemptAsync(otherVisitor, path, null);

            Assert.NotEqual(HttpStatusCode.TooManyRequests, answer.StatusCode);
        }

        [Theory]
        [InlineData(LogInPath, 10)]
        [InlineData(ChangePasswordPath, 10)]
        [InlineData(AuthRequests.RegisterPath, 5)]
        public async Task ClientSentXForwardedFor_DoesNotChangeTheCountedAddress(string path, int allowed)
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            for (int attempt = 1; attempt <= allowed; attempt++)
            {
                await RateLimitRequests.PostCountedAttemptAsync(client, path, $"198.51.100.{attempt}");
            }

            HttpResponseMessage refused = await RateLimitRequests.PostCountedAttemptAsync(client, path, "198.51.100.200");

            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        }

        [Fact]
        public async Task LogOut_ManyTimesFromOneAddress_IsNeverRefused()
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());

            for (int attempt = 0; attempt < 20; attempt++)
            {
                HttpResponseMessage response = await AuthRequests.LogOutAsync(client);

                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }

        [Fact]
        public async Task Me_ManyTimesFromOneAddress_IsNeverRefused()
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());

            for (int attempt = 0; attempt < 20; attempt++)
            {
                HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }

        private static async Task AssertNotRefusedAsync(HttpClient client, string path, int count)
        {
            for (int attempt = 1; attempt <= count; attempt++)
            {
                HttpResponseMessage response = await RateLimitRequests.PostCountedAttemptAsync(client, path, null);

                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }
    }
}
