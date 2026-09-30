using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Kvit.Api.Tests.Hosting
{
    public static class TestCertificate
    {
        private static readonly Lazy<(string Base64, string Password)> Created = new(Create);

        public static string Base64 => Created.Value.Base64;

        public static string Password => Created.Value.Password;

        private static (string Base64, string Password) Create()
        {
            string password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            DateTimeOffset now = DateTimeOffset.UtcNow;

            using RSA key = RSA.Create(2048);
            CertificateRequest request = new("CN=Kvit test certificate", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using X509Certificate2 certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(1));

            byte[] pfx = certificate.Export(X509ContentType.Pfx, password);
            return (Convert.ToBase64String(pfx), password);
        }
    }
}
