using System.Net;
using Kvit.Api.Tests.Hosting;
using Xunit;

namespace Kvit.Api.Tests.Startup
{
    public class DatabaseSettingTests
    {
        private const string ConnectionStringKey = "ConnectionStrings:KvitDatabase";
        private const string ClosedPortConnectionString = "Host=127.0.0.1;Port=1;Database=kvit;Username=kvit;Timeout=2";

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Start_WithoutAConnectionString_FailsAndNamesTheSetting(string? connectionString)
        {
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            Assert.Contains(ConnectionStringKey, MessagesOf(exception));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Start_WithABlankConnectionString_ThrowsAnInvalidOperationExceptionNamingTheSetting(string connectionString)
        {
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(connectionString);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            Assert.Contains(
                ExceptionChainOf(exception),
                candidate => candidate is InvalidOperationException && candidate.Message.Contains(ConnectionStringKey));
        }

        [Theory]
        [InlineData("/health")]
        [InlineData("/api/health")]
        public async Task Health_WithTheDatabaseOnAClosedPort_StillAnswersHealthy(string path)
        {
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(ClosedPortConnectionString);
            using HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", body);
        }

        private static List<Exception> ExceptionChainOf(Exception exception)
        {
            List<Exception> chain = [];
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                chain.Add(current);
            }

            return chain;
        }

        private static string MessagesOf(Exception exception)
        {
            return string.Join(Environment.NewLine, ExceptionChainOf(exception).Select(candidate => candidate.Message));
        }
    }
}
