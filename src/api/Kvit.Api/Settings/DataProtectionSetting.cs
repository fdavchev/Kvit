using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Kvit.Api.Settings
{
    public static class DataProtectionSetting
    {
        private const string CertificateBase64Key = "DataProtection:CertificateBase64";
        private const string CertificatePasswordKey = "DataProtection:CertificatePassword";
        public const int ExpiryWarningDays = 60;
        private const string HowToFix =
            "For local work, run from the repository root: dotnet run scripts/NewDataProtectionCertificate.cs " +
            "(see docs/guides/phase-04-local-setup.md, Part 3). " +
            "On a server, set the environment variables DataProtection__CertificateBase64 and DataProtection__CertificatePassword.";

        public static X509Certificate2 LoadCertificate(IConfiguration configuration, DateTimeOffset now)
        {
            string certificateBase64 = ReadRequired(configuration, CertificateBase64Key);
            string certificatePassword = ReadRequired(configuration, CertificatePasswordKey);

            X509Certificate2 certificate;
            try
            {
                byte[] pfx = Convert.FromBase64String(certificateBase64);
                certificate = X509CertificateLoader.LoadPkcs12(pfx, certificatePassword, X509KeyStorageFlags.EphemeralKeySet);
            }
            catch (Exception exception) when (exception is FormatException or CryptographicException)
            {
                throw new InvalidOperationException(
                    $"The certificate in {CertificateBase64Key} cannot be opened with the password in {CertificatePasswordKey}: {exception.Message} " +
                    $"The value must be the base64 of a PFX file and the password must be the one it was exported with. {HowToFix}",
                    exception);
            }

            if (!certificate.HasPrivateKey)
            {
                certificate.Dispose();
                throw new InvalidOperationException(
                    $"The certificate in {CertificateBase64Key} (opened with {CertificatePasswordKey}) has no private key, " +
                    $"so Kvit could not read back the login keys it encrypts with it. {HowToFix}");
            }

            DateTimeOffset notBefore = new(certificate.NotBefore);
            DateTimeOffset notAfter = new(certificate.NotAfter);
            if (now < notBefore || now > notAfter)
            {
                certificate.Dispose();
                throw new InvalidOperationException(
                    $"The certificate in {CertificateBase64Key} is valid only from {IsoDateOf(notBefore)} to {IsoDateOf(notAfter)}, " +
                    $"but it is now {IsoDateOf(now)}, so Kvit will not protect its login keys with it. " +
                    $"Make a new certificate that keeps the old login keys readable. {HowToFix}");
            }

            return certificate;
        }

        public static void WarnIfCertificateExpiresSoon(X509Certificate2 certificate, ILogger logger, DateTimeOffset now)
        {
            DateTimeOffset notAfter = new(certificate.NotAfter);
            TimeSpan left = notAfter - now;
            if (left > TimeSpan.FromDays(ExpiryWarningDays))
            {
                return;
            }

            logger.LogWarning(
                "The login-key certificate in {CertificateSetting} expires on {NotAfter}, in {DaysLeft} days. When it expires Kvit stops at start-up. Make a new certificate before then. {HowToFix}",
                CertificateBase64Key,
                IsoDateOf(notAfter),
                (int)Math.Floor(left.TotalDays),
                HowToFix);
        }

        private static string IsoDateOf(DateTimeOffset moment)
        {
            return moment.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }

        private static string ReadRequired(IConfiguration configuration, string key)
        {
            string? value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"The setting {key} is missing or empty, so Kvit cannot protect its login keys. {HowToFix}");
            }

            return value;
        }
    }
}
