using Kvit.Api.Tests.Postgres;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Kvit.Api.Tests.Hosting
{
    public class KvitApiFactory : WebApplicationFactory<Program>
    {
        private const string ConnectionStringKey = "ConnectionStrings:KvitDatabase";
        private const string CertificateBase64Key = "DataProtection:CertificateBase64";
        private const string CertificatePasswordKey = "DataProtection:CertificatePassword";
        public const string UnreachableConnectionString = "Host=127.0.0.1;Port=1;Database=kvit;Username=kvit;Timeout=2";

        private readonly Lazy<string?> _connectionString;
        private readonly string? _certificateBase64;
        private readonly string? _certificatePassword;

        public KvitApiFactory(PostgresFixture postgres)
            : this(new Lazy<string?>(postgres.CreateDatabase), TestCertificate.Base64, TestCertificate.Password)
        {
        }

        private KvitApiFactory(Lazy<string?> connectionString, string? certificateBase64, string? certificatePassword)
        {
            _connectionString = connectionString;
            _certificateBase64 = certificateBase64;
            _certificatePassword = certificatePassword;
        }

        public static KvitApiFactory WithConnectionString(string? connectionString)
        {
            return new KvitApiFactory(new Lazy<string?>(() => connectionString), TestCertificate.Base64, TestCertificate.Password);
        }

        public static KvitApiFactory WithDataProtectionSettings(string? certificateBase64, string? certificatePassword)
        {
            return new KvitApiFactory(new Lazy<string?>(() => UnreachableConnectionString), certificateBase64, certificatePassword);
        }

        public KvitApiFactory CreateAnotherInstanceOnTheSameDatabase()
        {
            return new KvitApiFactory(_connectionString, _certificateBase64, _certificatePassword);
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
                ]));

            return base.CreateHost(builder);
        }
    }
}
