using System.Net;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class ClosedByDefaultTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const string UnprotectedPath = "/api/test/unprotected";

        [Fact]
        public async Task EndpointWithoutAnyAuthorizationAttribute_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClientWithTestControllers();

            HttpResponseMessage response = await client.GetAsync(UnprotectedPath, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task EndpointWithoutAnyAuthorizationAttribute_Anonymous_DoesNotRedirectToALoginPage()
        {
            HttpClient client = _app.CreateClientWithTestControllers();

            HttpResponseMessage response = await client.GetAsync(UnprotectedPath, TestContext.Current.CancellationToken);

            Assert.Null(response.Headers.Location);
        }

        [Fact]
        public async Task EndpointWithoutAnyAuthorizationAttribute_LoggedIn_Answers200()
        {
            HttpClient client = _app.CreateClientWithTestControllers();
            RegistrationForm form = RegistrationForm.Valid();
            HttpResponseMessage registered = await AuthRequests.RegisterAsync(client, form);
            Assert.Equal(HttpStatusCode.OK, registered.StatusCode);

            HttpResponseMessage response = await client.GetAsync(UnprotectedPath, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("open", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task EndpointWithoutAnyAuthorizationAttribute_AfterLogOut_Answers401()
        {
            HttpClient client = _app.CreateClientWithTestControllers();
            await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());
            await AuthRequests.LogOutAsync(client);

            HttpResponseMessage response = await client.GetAsync(UnprotectedPath, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
