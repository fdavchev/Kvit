using System.Net;
using System.Text;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Proxy;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.RateLimiting
{
    public class RateLimitTests(ProxiedApp _app) : IClassFixture<ProxiedApp>
    {
        private const string LogInPath = "/api/auth/login";
        private const string RegisterPath = "/api/auth/register";
        private const string ForwardedForHeader = "X-Forwarded-For";

        [Theory]
        [InlineData(LogInPath, 10)]
        [InlineData(RegisterPath, 5)]
        public async Task Request_AfterTheAllowedNumberFromOneAddress_Answers429RateLimited(string path, int allowed)
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());

            await AssertNotRefusedAsync(client, path, allowed);
            HttpResponseMessage refused = await PostEmptyJsonAsync(client, path, null);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.TooManyRequests, ResultCodes.RATE_LIMITED);
        }

        [Theory]
        [InlineData(LogInPath, 10)]
        [InlineData(RegisterPath, 5)]
        public async Task RefusedRequest_CarriesARetryAfterInWholeSeconds(string path, int allowed)
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            await AssertNotRefusedAsync(client, path, allowed);

            HttpResponseMessage refused = await PostEmptyJsonAsync(client, path, null);

            Assert.True(refused.Headers.TryGetValues("Retry-After", out IEnumerable<string>? values));
            Assert.True(int.TryParse(values.Single(), out int seconds), $"Retry-After is not whole seconds: {values.Single()}");
            Assert.True(seconds > 0, $"Retry-After must be greater than zero, got {seconds}");
        }

        [Theory]
        [InlineData(LogInPath, 10)]
        [InlineData(RegisterPath, 5)]
        public async Task AnotherAddress_IsStillAllowedWhileOneAddressIsRefused(string path, int allowed)
        {
            HttpClient refusedVisitor = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            HttpClient otherVisitor = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            await AssertNotRefusedAsync(refusedVisitor, path, allowed);
            HttpResponseMessage refused = await PostEmptyJsonAsync(refusedVisitor, path, null);
            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);

            HttpResponseMessage answer = await PostEmptyJsonAsync(otherVisitor, path, null);

            Assert.NotEqual(HttpStatusCode.TooManyRequests, answer.StatusCode);
        }

        [Theory]
        [InlineData(LogInPath, 10)]
        [InlineData(RegisterPath, 5)]
        public async Task ClientSentXForwardedFor_DoesNotChangeTheCountedAddress(string path, int allowed)
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            for (int attempt = 1; attempt <= allowed; attempt++)
            {
                await PostEmptyJsonAsync(client, path, $"198.51.100.{attempt}");
            }

            HttpResponseMessage refused = await PostEmptyJsonAsync(client, path, "198.51.100.200");

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
                HttpResponseMessage response = await PostEmptyJsonAsync(client, path, null);

                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }

        private static async Task<HttpResponseMessage> PostEmptyJsonAsync(HttpClient client, string path, string? forwardedFor)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, path)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
            if (forwardedFor is not null)
            {
                request.Headers.Add(ForwardedForHeader, forwardedFor);
            }

            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }
    }
}
