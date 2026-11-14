using Kvit.Api.Settings;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kvit.Api.Tests.Startup
{
    public class WebsiteSettingTests
    {
        private const string OriginKey = "Website:Origin";

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("kvit-mk.pages.dev")]
        [InlineData("/groups")]
        [InlineData("ftp://kvit-mk.pages.dev")]
        [InlineData("http://kvit-mk.pages.dev")]
        [InlineData("https://kvit-mk.pages.dev/groups")]
        [InlineData("https://kvit-mk.pages.dev/?x=1")]
        [InlineData("https://kvit-mk.pages.dev/#top")]
        [InlineData("https://someone@kvit-mk.pages.dev")]
        public void ReadOrigin_WithoutAnHttpsOrigin_ThrowsAnInvalidOperationExceptionNamingTheSetting(string? origin)
        {
            IConfiguration configuration = ConfigurationWithOrigin(origin);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WebsiteSetting.ReadOrigin(configuration));

            Assert.Contains(OriginKey, exception.Message);
        }

        [Theory]
        [InlineData("https://kvit-mk.pages.dev", "https://kvit-mk.pages.dev")]
        [InlineData("https://kvit-mk.pages.dev/", "https://kvit-mk.pages.dev")]
        [InlineData("http://localhost:5173", "http://localhost:5173")]
        [InlineData("http://127.0.0.1:5173", "http://127.0.0.1:5173")]
        public void ReadOrigin_WithAnAllowedOrigin_ReturnsIt(string origin, string expectedAuthority)
        {
            IConfiguration configuration = ConfigurationWithOrigin(origin);

            Uri address = WebsiteSetting.ReadOrigin(configuration);

            Assert.Equal(expectedAuthority, address.GetLeftPart(UriPartial.Authority));
        }

        private static IConfigurationRoot ConfigurationWithOrigin(string? origin)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection([new KeyValuePair<string, string?>(OriginKey, origin)])
                .Build();
        }
    }
}
