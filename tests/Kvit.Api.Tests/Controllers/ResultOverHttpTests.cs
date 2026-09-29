using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Controllers
{
    public class ResultOverHttpTests(WebApplicationFactory<Program> _factory) : IClassFixture<WebApplicationFactory<Program>>
    {
        [Fact]
        public async Task SuccessfulResultWithValue_Answers200WithAJsonBody()
        {
            HttpClient client = CreateClient();

            HttpResponseMessage response = await client.GetAsync("/test-results/value", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("{\"name\":\"Ana\"}", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task SuccessfulResultWithNullValue_Answers204WithoutBodyOrContentType()
        {
            HttpClient client = CreateClient();

            HttpResponseMessage response = await client.GetAsync("/test-results/null-value", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(response.Content.Headers.ContentType);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        private HttpClient CreateClient()
        {
            return _factory
                .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                    services.AddControllers().AddApplicationPart(typeof(ResultEndpointsTestController).Assembly)))
                .CreateClient();
        }
    }
}
