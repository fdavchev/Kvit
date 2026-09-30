using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Postgres;
using Microsoft.Extensions.Hosting;

namespace Kvit.Api.Tests.Proxy
{
    public class ProxiedApp : AuthApp
    {
        public const string Secret = "test-proxy-secret-0123456789abcdef";
        public const string SecretHeader = "X-Kvit-Proxy-Secret";
        public const string VisitorAddressHeader = "X-Kvit-Visitor-Ip";

        private readonly KvitApiFactory _proxiedFactory;
        private int _visitorCount;

        public ProxiedApp(PostgresFixture postgres)
            : base(postgres)
        {
            _proxiedFactory = _factory.CreateAnotherInstanceWithProxySecret(Secret);
        }

        public override async ValueTask DisposeAsync()
        {
            await _proxiedFactory.DisposeAsync();
            await base.DisposeAsync();
        }

        public string NewVisitorAddress()
        {
            int number = Interlocked.Increment(ref _visitorCount);

            return $"10.0.{number / 256}.{number % 256}";
        }

        public HttpClient CreateClientSending(string? proxySecret, string? visitorAddress)
        {
            return WithHeaders(_proxiedFactory.CreateClient(HttpsWithCookies()), proxySecret, visitorAddress);
        }

        public HttpClient CreateProductionClientSending(string? proxySecret, string? visitorAddress)
        {
            HttpClient client = _proxiedFactory.InEnvironment(Environments.Production).CreateClient(HttpsWithCookies());

            return WithHeaders(client, proxySecret, visitorAddress);
        }

        private static HttpClient WithHeaders(HttpClient client, string? proxySecret, string? visitorAddress)
        {
            if (proxySecret is not null)
            {
                client.DefaultRequestHeaders.Add(SecretHeader, proxySecret);
            }

            if (visitorAddress is not null)
            {
                client.DefaultRequestHeaders.Add(VisitorAddressHeader, visitorAddress);
            }

            return client;
        }
    }
}
