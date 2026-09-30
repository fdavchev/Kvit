using Kvit.Api.Tests.Postgres;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kvit.Api.Tests.Hosting
{
    public class KvitApiFactory : WebApplicationFactory<Program>
    {
        private const string ConnectionStringKey = "ConnectionStrings:KvitDatabase";
        private const string CertificateBase64Key = "DataProtection:CertificateBase64";
        private const string CertificatePasswordKey = "DataProtection:CertificatePassword";
        private const string ProxySecretKey = "Proxy:SharedSecret";
        public const string UnreachableConnectionString = "Host=127.0.0.1;Port=1;Database=kvit;Username=kvit;Timeout=2";

        private readonly Lazy<string?> _connectionString;
        private readonly string? _certificateBase64;
        private readonly string? _certificatePassword;
        private readonly string? _proxySecret;

        public KvitApiFactory(PostgresFixture postgres)
            : this(new Lazy<string?>(postgres.CreateDatabase), TestCertificate.Base64, TestCertificate.Password, null)
        {
        }

        private KvitApiFactory(Lazy<string?> connectionString, string? certificateBase64, string? certificatePassword, string? proxySecret)
        {
            _connectionString = connectionString;
            _certificateBase64 = certificateBase64;
            _certificatePassword = certificatePassword;
            _proxySecret = proxySecret;
        }

        public static KvitApiFactory WithConnectionString(string? connectionString)
        {
            return new KvitApiFactory(new Lazy<string?>(() => connectionString), TestCertificate.Base64, TestCertificate.Password, null);
        }

        public static KvitApiFactory WithDataProtectionSettings(string? certificateBase64, string? certificatePassword)
        {
            return new KvitApiFactory(new Lazy<string?>(() => UnreachableConnectionString), certificateBase64, certificatePassword, null);
        }

        public static KvitApiFactory WithProxySecret(string? proxySecret)
        {
            return new KvitApiFactory(new Lazy<string?>(() => UnreachableConnectionString), TestCertificate.Base64, TestCertificate.Password, proxySecret);
        }

        public KvitApiFactory CreateAnotherInstanceOnTheSameDatabase()
        {
            return new KvitApiFactory(_connectionString, _certificateBase64, _certificatePassword, _proxySecret);
        }

        public KvitApiFactory CreateAnotherInstanceWithProxySecret(string proxySecret)
        {
            return new KvitApiFactory(_connectionString, _certificateBase64, _certificatePassword, proxySecret);
        }

        public WebApplicationFactory<Program> InEnvironment(string environmentName)
        {
            return WithWebHostBuilder(builder => builder.UseEnvironment(environmentName));
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            string? connectionString = _connectionString.Value;

            builder.ConfigureHostConfiguration(configuration =>
                configuration.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>(ConnectionStringKey, connectionString),
                    new KeyValuePair<string, string?>(CertificateBase64Key, _certificateBase64),
                    new KeyValuePair<string, string?>(CertificatePasswordKey, _certificatePassword),
                    new KeyValuePair<string, string?>(ProxySecretKey, _proxySecret),
                ]));

            if (string.IsNullOrWhiteSpace(_proxySecret))
            {
                builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, UniqueClientAddressStartupFilter>());
            }

            return base.CreateHost(builder);
        }
    }
}
