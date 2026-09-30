using System.Security.Cryptography.X509Certificates;
using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;

namespace Kvit.Api.Registers
{
    public static partial class Register
    {
        private const string DataProtectionApplicationName = "Kvit";

        public static IServiceCollection AddDataProtectionKeys(this IServiceCollection services, X509Certificate2 certificate)
        {
            services.AddDataProtection()
                .SetApplicationName(DataProtectionApplicationName)
                .PersistKeysToDbContext<AppDbContext>()
                .ProtectKeysWithCertificate(certificate);

            return services;
        }
    }
}
