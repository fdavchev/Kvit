using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Proxy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kvit.Api.Tests.Wake
{
    public sealed class WakeApp : IAsyncDisposable
    {
        public const string DevelopmentSiteOrigin = "http://localhost:5173";
        public const string ProductionSiteOrigin = "https://kvit-mk.pages.dev";

        private readonly KvitApiFactory _withoutDatabase = KvitApiFactory.WithConnectionString(KvitApiFactory.UnreachableConnectionString);
        private readonly KvitApiFactory _behindTheProxyGate = KvitApiFactory.WithProxySecret(ProxiedApp.Secret);

        public HttpClient CreateClient()
        {
            return _withoutDatabase.CreateClient(AuthApp.HttpsWithoutCookies());
        }

        public HttpClient CreateProductionClientWithoutTheProxySecret()
        {
            WebApplicationFactory<Program> production = _behindTheProxyGate.InEnvironment(Environments.Production);

            return production.CreateClient(AuthApp.HttpsWithoutCookies());
        }

        public static Task<HttpResponseMessage> GetAsync(HttpClient client, string pathAndQuery)
        {
            return client.GetAsync(pathAndQuery, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> WakeWithReturnToAsync(HttpClient client, string returnTo)
        {
            return GetAsync(client, $"/wake?returnTo={Uri.EscapeDataString(returnTo)}");
        }

        public async ValueTask DisposeAsync()
        {
            await _withoutDatabase.DisposeAsync();
            await _behindTheProxyGate.DisposeAsync();
        }
    }
}
