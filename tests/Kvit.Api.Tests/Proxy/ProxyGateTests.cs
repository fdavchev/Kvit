using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Hosting;
using Xunit;

namespace Kvit.Api.Tests.Proxy
{
    public class ProxyGateTests(ProxiedApp _app) : IClassFixture<ProxiedApp>
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-the-secret")]
        [InlineData(ProxiedApp.Secret + "x")]
        public async Task Request_WithAWrongOrMissingSecretAndAFakeVisitorAddress_Answers403WithAnEmptyBody(string? proxySecret)
        {
            HttpClient client = _app.CreateClientSending(proxySecret, _app.NewVisitorAddress());

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData("/health")]
        [InlineData("/api/health")]
        public async Task Health_WithoutTheSecretAndWithoutAVisitorAddress_StillAnswersHealthy(string path)
        {
            HttpClient client = _app.CreateClientSending(null, null);

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Request_WithTheRightSecretAndAVisitorAddress_ReachesTheApp()
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SignUpAndLogIn_WithTheRightSecretAndAVisitorAddress_Work()
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            RegistrationForm form = RegistrationForm.Valid();

            HttpResponseMessage registered = await AuthRequests.RegisterAsync(client, form);
            HttpResponseMessage loggedIn = await AuthRequests.LogInAsync(client, form.Email, form.Password);

            Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
            Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-an-ip")]
        [InlineData("203.0.113.1, 203.0.113.2")]
        public async Task Request_WithTheRightSecretButNoSingleVisitorAddress_FailsWithAnInvalidOperationException(string? visitorAddress)
        {
            HttpClient client = _app.CreateProductionClientSending(ProxiedApp.Secret, visitorAddress);

            Exception exception = await Assert.ThrowsAnyAsync<Exception>(() => AuthRequests.GetMeAsync(client));

            Assert.Contains(ExceptionChain.Of(exception), candidate => candidate is InvalidOperationException);
        }
    }
}
