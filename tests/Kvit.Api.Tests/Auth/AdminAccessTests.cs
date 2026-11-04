using System.Net;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class AdminAccessTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const string AdminOnlyPath = "/api/test/admin-only";

        [Fact]
        public async Task AdminEndpoint_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClientWithTestControllers();

            HttpResponseMessage response = await client.GetAsync(AdminOnlyPath, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AdminEndpoint_SignedInWithoutTheAdminRole_Answers403()
        {
            HttpClient client = _app.CreateClientWithTestControllers();
            await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());

            HttpResponseMessage response = await client.GetAsync(AdminOnlyPath, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AdminEndpoint_LoggedInAfterBeingMadeAdmin_Answers200()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            await _app.MakeAdminAsync(form.Email);
            HttpClient client = _app.CreateClientWithTestControllers();
            await AuthRequests.LogInAsync(client, form.Email, form.Password);

            HttpResponseMessage response = await client.GetAsync(AdminOnlyPath, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("admin", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AdminEndpoint_SignedInBeforeBeingMadeAdmin_Answers200OnTheNextCallWithoutLoggingInAgain()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = _app.CreateClientWithTestControllers();
            await AuthRequests.RegisterAsync(client, form);
            HttpResponseMessage before = await client.GetAsync(AdminOnlyPath, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, before.StatusCode);

            await _app.MakeAdminAsync(form.Email);

            HttpResponseMessage after = await client.GetAsync(AdminOnlyPath, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        }

        [Fact]
        public async Task AdminEndpoint_AdminWhoIsRevokedWhileSignedIn_Answers403OnTheNextCall()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            await _app.MakeAdminAsync(form.Email);
            HttpClient client = _app.CreateClientWithTestControllers();
            await AuthRequests.LogInAsync(client, form.Email, form.Password);
            HttpResponseMessage before = await client.GetAsync(AdminOnlyPath, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, before.StatusCode);

            await _app.RevokeAdminAsync(form.Email);

            HttpResponseMessage after = await client.GetAsync(AdminOnlyPath, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, after.StatusCode);
        }

        [Fact]
        public async Task AdminEndpoint_AfterLogOut_Answers401()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            await _app.MakeAdminAsync(form.Email);
            HttpClient client = _app.CreateClientWithTestControllers();
            await AuthRequests.LogInAsync(client, form.Email, form.Password);
            await AuthRequests.LogOutAsync(client);

            HttpResponseMessage response = await client.GetAsync(AdminOnlyPath, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
