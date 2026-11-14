using System.Net;
using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Proxy;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupDatabaseDownTests
    {
        [Fact]
        public async Task Post_SecretNotConfigured_Answers404WithoutTouchingTheDatabase()
        {
            using KvitApiFactory factory = KvitApiFactory.WithConnectionString(KvitApiFactory.UnreachableConnectionString);
            using HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await CleanupRequests.PostAsync(client, CleanupApp.Secret);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Post_NoHeader_Answers403WithoutTouchingTheDatabase()
        {
            using KvitApiFactory factory = FactoryWithTheSecretAndNoDatabase();
            using HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await CleanupRequests.PostWithoutSecretAsync(client);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Post_WrongSecret_Answers403WithoutTouchingTheDatabase()
        {
            using KvitApiFactory factory = FactoryWithTheSecretAndNoDatabase();
            using HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await CleanupRequests.PostAsync(client, "wrong-secret");

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Post_RightSecretWhileTheDatabaseIsDown_Answers500NotAnEmptyReport()
        {
            using KvitApiFactory factory = FactoryWithTheSecretAndNoDatabase();
            using HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await CleanupRequests.PostAsync(client, CleanupApp.Secret);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Theory]
        [InlineData("/health")]
        [InlineData("/api/health")]
        public async Task Health_WithTheSecretSetAndTheDatabaseDown_StillAnswersHealthy(string path)
        {
            using KvitApiFactory factory = FactoryWithTheSecretAndNoDatabase();
            using HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Start_InProductionWithoutTheCleanupSecret_Starts()
        {
            using KvitApiFactory factory = KvitApiFactory.WithProxySecret("a-secret-for-this-test");

            using HttpClient client = factory.InEnvironment(Environments.Production).CreateClient();

            Assert.NotNull(client);
        }

        [Fact]
        public async Task Post_InProductionWithoutTheCleanupSecret_Answers404()
        {
            using KvitApiFactory factory = KvitApiFactory.WithProxySecret(ProxiedApp.Secret);
            using HttpClient client = factory.InEnvironment(Environments.Production).CreateClient();
            client.DefaultRequestHeaders.Add(ProxiedApp.SecretHeader, ProxiedApp.Secret);
            client.DefaultRequestHeaders.Add(ProxiedApp.VisitorAddressHeader, "10.9.9.9");

            HttpResponseMessage response = await CleanupRequests.PostAsync(client, CleanupApp.Secret);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.NotFound);
        }

        private static KvitApiFactory FactoryWithTheSecretAndNoDatabase()
        {
            KvitApiFactory factory = KvitApiFactory.WithConnectionString(KvitApiFactory.UnreachableConnectionString);
            factory.CleanupSecret = CleanupApp.Secret;

            return factory;
        }
    }
}
