using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kvit.Api.Tests.OpenApi
{
    public class ApiReferenceEndpointTests(WebApplicationFactory<Program> _factory) : IClassFixture<WebApplicationFactory<Program>>
    {
        [Theory]
        [InlineData("/openapi/v1.json")]
        [InlineData("/scalar")]
        public async Task ApiReference_InDevelopment_AnswersWithStatus200(string path)
        {
            HttpClient client = ClientFor(Environments.Development);

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("/openapi/v1.json")]
        [InlineData("/scalar")]
        public async Task ApiReference_InProduction_AnswersWithStatus404(string path)
        {
            HttpClient client = ClientFor(Environments.Production);

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private HttpClient ClientFor(string environmentName)
        {
            return _factory
                .WithWebHostBuilder(builder => builder.UseEnvironment(environmentName))
                .CreateClient();
        }
    }
}
