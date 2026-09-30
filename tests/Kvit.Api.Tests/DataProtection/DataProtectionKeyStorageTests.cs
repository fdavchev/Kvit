using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Hosting;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Kvit.Api.Tests.DataProtection
{
    public class DataProtectionKeyStorageTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const string SelectKeyXml = "SELECT xml FROM data_protection_keys";
        private const string EncryptedSecretElement = "encryptedSecret";
        private const string XmlEncryptionNamespace = "http://www.w3.org/2001/04/xmlenc#";
        private const string PlainMasterKeyElement = "masterKey";
        private const string PlainValueElement = "<value>";

        [Fact]
        public async Task Register_IssuingTheFirstLoginCookie_StoresAKeyInTheDatabase()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<string?> keys = await _app.QueryAsync(SelectKeyXml);
            Assert.NotEmpty(keys);
        }

        [Fact]
        public async Task Register_StoredKeys_AreMarkedAsEncryptedWithXmlEncryption()
        {
            HttpClient client = _app.CreateClient();

            await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());

            List<string?> keys = await _app.QueryAsync(SelectKeyXml);
            Assert.NotEmpty(keys);
            Assert.All(keys, xml =>
            {
                Assert.NotNull(xml);
                Assert.Contains(EncryptedSecretElement, xml);
                Assert.Contains(XmlEncryptionNamespace, xml);
            });
        }

        [Fact]
        public async Task Register_StoredKeys_HoldNoPlainKeyMaterial()
        {
            HttpClient client = _app.CreateClient();

            await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());

            List<string?> keys = await _app.QueryAsync(SelectKeyXml);
            Assert.NotEmpty(keys);
            Assert.All(keys, xml =>
            {
                Assert.NotNull(xml);
                Assert.DoesNotContain(PlainMasterKeyElement, xml);
                Assert.DoesNotContain(PlainValueElement, xml);
            });
        }

        [Fact]
        public async Task Cookie_IssuedByOneInstance_IsAcceptedByAnotherInstanceOnTheSameDatabase()
        {
            HttpClient firstClient = _app.CreateClient();
            HttpResponseMessage registered = await AuthRequests.RegisterAsync(firstClient, RegistrationForm.Valid());
            Guid userId = (await AuthRequests.ReadJsonAsync(registered)).GetProperty("id").GetGuid();
            SetCookieHeaderValue? cookie = AuthCookie.Find(registered);
            Assert.NotNull(cookie);
            using KvitApiFactory secondInstance = _app.CreateAnotherInstance();
            using HttpClient secondClient = secondInstance.CreateClient(AuthApp.HttpsWithoutCookies());
            secondClient.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={cookie.Value}");

            HttpResponseMessage response = await AuthRequests.GetMeAsync(secondClient);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(userId, body.GetProperty("id").GetGuid());
            List<string?> keys = await _app.QueryAsync(SelectKeyXml);
            Assert.NotEmpty(keys);
        }
    }
}
