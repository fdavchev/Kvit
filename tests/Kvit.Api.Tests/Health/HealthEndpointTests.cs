using System.Net;
using Kvit.Api.Tests.Hosting;
using Xunit;

namespace Kvit.Api.Tests.Health
{
    public class HealthEndpointTests(KvitApiFactory _factory) : IClassFixture<KvitApiFactory>
    {
        [Theory]
        [InlineData("/health")]
        [InlineData("/api/health")]
        public async Task HealthEndpoint_AnswersHealthyWithStatus200(string path)
        {
            HttpClient client = _factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", body);
        }
    }
}
