using Kvit.Api.Tests.Postgres;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Kvit.Api.Tests.Hosting
{
    public class KvitApiFactory : WebApplicationFactory<Program>
    {
        private const string ConnectionStringKey = "ConnectionStrings:KvitDatabase";

        private readonly Lazy<string?> _connectionString;

        public KvitApiFactory(PostgresFixture postgres)
        {
            _connectionString = new Lazy<string?>(postgres.CreateDatabase);
        }

        private KvitApiFactory(string? connectionString)
        {
            _connectionString = new Lazy<string?>(() => connectionString);
        }

        public static KvitApiFactory WithConnectionString(string? connectionString)
        {
            return new KvitApiFactory(connectionString);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            string? connectionString = _connectionString.Value;

            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection([new KeyValuePair<string, string?>(ConnectionStringKey, connectionString)]));
        }
    }
}
