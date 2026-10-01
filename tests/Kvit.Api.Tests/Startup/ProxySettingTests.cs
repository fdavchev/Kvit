using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Hosting;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kvit.Api.Tests.Startup
{
    public class ProxySettingTests
    {
        private const string ProxySecretKey = "Proxy:SharedSecret";

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Start_InProductionWithoutTheProxySecret_ThrowsAnInvalidOperationExceptionNamingTheSetting(string? proxySecret)
        {
            using KvitApiFactory factory = KvitApiFactory.WithProxySecret(proxySecret);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.InEnvironment(Environments.Production).CreateClient());

            Assert.Contains(
                ExceptionChain.Of(exception),
                candidate => candidate is InvalidOperationException && candidate.Message.Contains(ProxySecretKey));
        }

        [Fact]
        public void Start_InProductionWithTheProxySecret_Starts()
        {
            using KvitApiFactory factory = KvitApiFactory.WithProxySecret("a-secret-for-this-test");

            using HttpClient client = factory.InEnvironment(Environments.Production).CreateClient();

            Assert.NotNull(client);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        public async Task Start_OutsideProductionWithoutTheProxySecret_StartsWithTheGateOff(string environmentName)
        {
            using KvitApiFactory factory = KvitApiFactory.WithProxySecret(null);
            using HttpClient client = factory.InEnvironment(environmentName).CreateClient();

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
