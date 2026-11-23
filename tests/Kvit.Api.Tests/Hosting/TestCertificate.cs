using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Kvit.Api.Tests.Hosting
{
    public static class TestCertificate
    {
        private static readonly Lazy<(string Base64, string Password)> Created = new(Create);

        public static string Base64 => Created.Value.Base64;

        public static string Password => Created.Value.Password;

        public static (string Base64, string Password) ValidBetween(DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            string password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

            using RSA key = RSA.Create(2048);
            CertificateRequest request = new("CN=Kvit test certificate", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using X509Certificate2 certificate = request.CreateSelfSigned(notBefore, notAfter);

            byte[] pfx = certificate.Export(X509ContentType.Pfx, password);
            return (Convert.ToBase64String(pfx), password);
        }

        private static (string Base64, string Password) Create()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return ValidBetween(now.AddDays(-1), now.AddYears(1));
        }
    }
}
