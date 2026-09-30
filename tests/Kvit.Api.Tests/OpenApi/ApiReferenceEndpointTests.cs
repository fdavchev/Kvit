using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Proxy;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kvit.Api.Tests.OpenApi
{
    public class ApiReferenceEndpointTests(KvitApiFactory _factory, ProxiedApp _app) : IClassFixture<KvitApiFactory>, IClassFixture<ProxiedApp>
    {
        [Theory]
        [InlineData("/openapi/v1.json")]
        [InlineData("/scalar")]
        public async Task ApiReference_InDevelopment_AnswersWithStatus200(string path)
        {
            HttpClient client = _factory.InEnvironment(Environments.Development).CreateClient();

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("/openapi/v1.json")]
        [InlineData("/scalar")]
        public async Task ApiReference_InProduction_IsNotServedToAnAnonymousVisitor(string path)
        {
            HttpClient client = _app.CreateProductionClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/openapi/v1.json")]
        [InlineData("/scalar")]
        public async Task ApiReference_InProduction_AnswersNotFoundToASignedInVisitor(string path)
        {
            HttpClient client = _app.CreateProductionClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            HttpResponseMessage registered = await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());
            Assert.Equal(HttpStatusCode.OK, registered.StatusCode);

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
