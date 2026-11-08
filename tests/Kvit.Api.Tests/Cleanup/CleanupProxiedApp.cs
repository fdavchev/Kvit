using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Postgres;
using Kvit.Api.Tests.Proxy;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupProxiedApp : ProxiedApp
    {
        private readonly KvitApiFactory _cleanupFactory;

        public CleanupProxiedApp(PostgresFixture postgres)
            : base(postgres)
        {
            _cleanupFactory = _factory.CreateAnotherInstanceWithProxySecret(Secret);
            _cleanupFactory.CleanupSecret = CleanupApp.Secret;
        }

        public override async ValueTask DisposeAsync()
        {
            await _cleanupFactory.DisposeAsync();
            await base.DisposeAsync();
        }

        public HttpClient CreateCleanupClientSending(string? proxySecret, string? visitorAddress)
        {
            return WithHeaders(_cleanupFactory.CreateClient(HttpsWithoutCookies()), proxySecret, visitorAddress);
        }
    }
}
