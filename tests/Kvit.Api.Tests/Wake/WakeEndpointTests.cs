using System.Net;
using Kvit.Api.Wake;
using Xunit;

namespace Kvit.Api.Tests.Wake
{
    public class WakeEndpointTests(WakeApp _app) : IClassFixture<WakeApp>
    {
        private const string SiteHome = WakeApp.DevelopmentSiteOrigin + "/";

        public static TheoryData<string> LocalPaths =>
        [
            "/",
            "/groups",
            "/groups/3f2c9a1e-0b6d-4c1a-9d7e-2a4b5c6d7e8f",
            "/groups/3f2c9a1e-0b6d-4c1a-9d7e-2a4b5c6d7e8f/expenses/new?currency=EUR&split=equal",
            "/budget?month=2026-11",
            "/join/AbC-123_xyz",
            "/%D0%B3%D1%80%D1%83%D0%BF%D0%B0",
            "/@friends",
        ];

        public static TheoryData<string> UnsafeReturnTos =>
        [
            "",
            "//evil.com",
            "//evil.com/groups",
            "///evil.com",
            "/\\evil.com",
            "\\\\evil.com",
            "\\/evil.com",
            "https://evil.com",
            "http://evil.com/",
            "https:evil.com",
            "javascript:alert(1)",
            "evil.com",
            "@evil.com",
            "groups",
            "/groups//evil.com",
            "/\tevil.com",
            "/\t/evil.com",
            "/\r\nSet-Cookie: kvit=stolen",
            "/ evil.com",
            "/grüppe",
            "/\u0000",
            "/%2F%2Fevil.com\\",
        ];

        public static TheoryData<string> UnsafeRawQueries =>
        [
            "returnTo=%2F%2Fevil.com",
            "returnTo=%2f%2fevil.com",
            "returnTo=%2F%5Cevil.com",
            "returnTo=%5C%5Cevil.com",
            "returnTo=https%3A%2F%2Fevil.com",
            "returnTo=//evil.com",
            "returnTo=/%09/evil.com",
            "returnTo=/%0D%0ALocation:%20https://evil.com",
            "returnTo=%252F%252Fevil.com",
            "returnTo=%E2%81%84%E2%81%84evil.com",
            "returnTo=/groups&returnTo=//evil.com",
            "returnTo=/groups&returnTo=/budget",
            "returnto=//evil.com&returnTo=",
        ];

        [Fact]
        public async Task Wake_WithoutReturnTo_Answers302ToTheSiteHome()
        {
            HttpResponseMessage response = await WakeApp.GetAsync(_app.CreateClient(), "/wake");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(SiteHome, LocationOf(response));
        }

        [Theory]
        [MemberData(nameof(LocalPaths))]
        public async Task Wake_WithALocalPath_Answers302ToThatPathOnTheSite(string path)
        {
            HttpResponseMessage response = await WakeApp.WakeWithReturnToAsync(_app.CreateClient(), path);

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(WakeApp.DevelopmentSiteOrigin + path, LocationOf(response));
        }

        [Theory]
        [MemberData(nameof(UnsafeReturnTos))]
        public async Task Wake_WithAReturnToThatIsNotALocalPath_Answers302ToTheSiteHome(string returnTo)
        {
            HttpResponseMessage response = await WakeApp.WakeWithReturnToAsync(_app.CreateClient(), returnTo);

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(SiteHome, LocationOf(response));
        }

        [Theory]
        [MemberData(nameof(UnsafeRawQueries))]
        public async Task Wake_WithAnEncodedOrRepeatedReturnTo_Answers302ToTheSiteHome(string rawQuery)
        {
            HttpResponseMessage response = await WakeApp.GetAsync(_app.CreateClient(), $"/wake?{rawQuery}");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(SiteHome, LocationOf(response));
        }

        [Fact]
        public async Task Wake_WithAPathWhoseDecodedTextLooksLikeTwoSlashes_StaysOnTheSite()
        {
            HttpResponseMessage response = await WakeApp.GetAsync(_app.CreateClient(), "/wake?returnTo=/%252F%252Fevil.com");

            Uri location = new(LocationOf(response));
            Assert.Equal(new Uri(SiteHome).Authority, location.Authority);
            Assert.Equal("/%2F%2Fevil.com", location.PathAndQuery);
        }

        [Fact]
        public async Task Wake_WithALocalPathOfTheLongestAllowedLength_KeepsIt()
        {
            string path = "/" + new string('a', WakeReturnPath.MaxLength - 1);

            HttpResponseMessage response = await WakeApp.WakeWithReturnToAsync(_app.CreateClient(), path);

            Assert.Equal(WakeApp.DevelopmentSiteOrigin + path, LocationOf(response));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10_000)]
        public async Task Wake_WithALocalPathLongerThanAllowed_Answers302ToTheSiteHome(int charactersOverTheLimit)
        {
            string path = "/" + new string('a', WakeReturnPath.MaxLength - 1 + charactersOverTheLimit);

            HttpResponseMessage response = await WakeApp.WakeWithReturnToAsync(_app.CreateClient(), path);

            Assert.Equal(SiteHome, LocationOf(response));
        }

        [Fact]
        public async Task Wake_Answers_CacheControlNoStore()
        {
            HttpResponseMessage response = await WakeApp.WakeWithReturnToAsync(_app.CreateClient(), "/groups");

            Assert.True(response.Headers.CacheControl?.NoStore, $"Cache-Control was '{response.Headers.CacheControl}'.");
        }

        [Fact]
        public async Task Wake_WithTheDatabaseDownAndNoLogin_StillAnswers302()
        {
            HttpResponseMessage response = await WakeApp.WakeWithReturnToAsync(_app.CreateClient(), "/groups");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        }

        [Fact]
        public async Task Wake_InProductionWithoutTheProxySecret_Answers302ToTheLiveSite()
        {
            HttpResponseMessage response = await WakeApp.WakeWithReturnToAsync(_app.CreateProductionClientWithoutTheProxySecret(), "/groups?tab=balances");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(WakeApp.ProductionSiteOrigin + "/groups?tab=balances", LocationOf(response));
        }

        [Fact]
        public async Task Wake_InProductionWithAnUnsafeReturnTo_Answers302ToTheLiveSiteHome()
        {
            HttpResponseMessage response = await WakeApp.WakeWithReturnToAsync(_app.CreateProductionClientWithoutTheProxySecret(), "//evil.com");

            Assert.Equal(WakeApp.ProductionSiteOrigin + "/", LocationOf(response));
        }

        [Fact]
        public async Task OtherApiPath_InProductionWithoutTheProxySecret_IsStillRefused()
        {
            HttpResponseMessage response = await WakeApp.GetAsync(_app.CreateProductionClientWithoutTheProxySecret(), "/wakeup");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task OtherMethod_IsNotRedirected(string method)
        {
            using HttpRequestMessage request = new(new HttpMethod(method), "/wake?returnTo=%2Fgroups");

            HttpResponseMessage response = await _app.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

            Assert.NotEqual(HttpStatusCode.Found, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }

        private static string LocationOf(HttpResponseMessage response)
        {
            Uri location = response.Headers.Location
                ?? throw new InvalidOperationException($"The answer {(int)response.StatusCode} has no Location header.");

            return location.OriginalString;
        }
    }
}
