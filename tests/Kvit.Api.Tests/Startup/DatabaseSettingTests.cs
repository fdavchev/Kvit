using System.Net;
using Kvit.Api.Tests.Hosting;
using Xunit;

namespace Kvit.Api.Tests.Startup
{
    public class DatabaseSettingTests
    {
        private const string ConnectionStringKey = "ConnectionStrings:KvitDatabase";

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Start_WithoutAConnectionString_ThrowsAnInvalidOperationExceptionNamingTheSetting(string? connectionString)
        {
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            Assert.Contains(
                ExceptionChain.Of(exception),
                candidate => candidate is InvalidOperationException && candidate.Message.Contains(ConnectionStringKey));
        }

        [Theory]
        [InlineData("/health")]
        [InlineData("/api/health")]
        public async Task Health_WithTheDatabaseOnAClosedPort_StillAnswersHealthy(string path)
        {
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(KvitApiFactory.UnreachableConnectionString);
            using HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", body);
        }
    }
}
