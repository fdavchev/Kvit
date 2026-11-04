using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Proxy;
using Kvit.Api.Tests.RateLimiting;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportRateLimitTests(ProxiedApp _app) : IClassFixture<ProxiedApp>
    {
        private const int Allowed = 10;

        [Fact]
        public async Task Export_UpToTheAllowedNumberFromOneAddress_AllAnswer200()
        {
            HttpClient client = await SignedInVisitorAsync();

            for (int call = 1; call <= Allowed; call++)
            {
                HttpResponseMessage response = await ExportRequests.GetAsync(client);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task Export_AfterTheAllowedNumberFromOneAddress_Answers429RateLimitedWithRetryAfter()
        {
            HttpClient client = await SignedInVisitorAsync();
            await ExportManyAsync(client, Allowed);

            HttpResponseMessage refused = await ExportRequests.GetAsync(client);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.TooManyRequests, ResultCodes.RATE_LIMITED);
            RateLimitRequests.AssertRetryAfterInWholeSeconds(refused);
        }

        [Fact]
        public async Task Export_AnotherAddress_IsStillAllowedWhileOneAddressIsRefused()
        {
            HttpClient refusedVisitor = await SignedInVisitorAsync();
            await ExportManyAsync(refusedVisitor, Allowed);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await ExportRequests.GetAsync(refusedVisitor)).StatusCode);
            HttpClient otherVisitor = await SignedInVisitorAsync();

            HttpResponseMessage response = await ExportRequests.GetAsync(otherVisitor);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Me_AfterTheExportLimitIsUsedUp_IsStillAnswered()
        {
            HttpClient client = await SignedInVisitorAsync();
            await ExportManyAsync(client, Allowed + 1);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<HttpClient> SignedInVisitorAsync()
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            HttpResponseMessage registered = await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());
            Assert.Equal(HttpStatusCode.OK, registered.StatusCode);

            return client;
        }

        private static async Task ExportManyAsync(HttpClient client, int count)
        {
            for (int call = 1; call <= count; call++)
            {
                await ExportRequests.GetAsync(client);
            }
        }
    }
}
