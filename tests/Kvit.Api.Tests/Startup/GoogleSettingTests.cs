using Kvit.Api.Settings;
using Kvit.Api.Tests.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kvit.Api.Tests.Startup
{
    public class GoogleSettingTests
    {
        private const string ClientIdKey = "Google:ClientId";

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Read_WithoutAClientId_ThrowsAnInvalidOperationExceptionNamingTheSetting(string? clientId)
        {
            IConfiguration configuration = ConfigurationWithClientId(clientId);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => GoogleSetting.Read(configuration));

            Assert.Contains(ClientIdKey, exception.Message);
        }

        [Fact]
        public void Read_WithAClientId_ReturnsIt()
        {
            IConfiguration configuration = ConfigurationWithClientId("123456-abc.apps.googleusercontent.com");

            string clientId = GoogleSetting.Read(configuration);

            Assert.Equal("123456-abc.apps.googleusercontent.com", clientId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Start_WithoutAClientId_ThrowsAnInvalidOperationExceptionNamingTheSetting(string? clientId)
        {
            using KvitApiFactory factory = KvitApiFactory.WithGoogleClientId(clientId);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            Assert.Contains(
                ExceptionChain.Of(exception),
                candidate => candidate is InvalidOperationException && candidate.Message.Contains(ClientIdKey));
        }

        private static IConfigurationRoot ConfigurationWithClientId(string? clientId)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection([new KeyValuePair<string, string?>(ClientIdKey, clientId)])
                .Build();
        }
    }
}
