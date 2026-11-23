using System.Globalization;
using System.Text;
using Kvit.Api.Tests.Cleanup;
using Kvit.Api.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kvit.Api.Tests.DataProtection
{
    public class DataProtectionSettingTests
    {
        private const string CertificateBase64Key = "DataProtection:CertificateBase64";
        private const string CertificatePasswordKey = "DataProtection:CertificatePassword";

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Start_WithoutACertificate_ThrowsAnInvalidOperationExceptionNamingTheSetting(string? certificateBase64)
        {
            using KvitApiFactory factory = KvitApiFactory.WithDataProtectionSettings(certificateBase64, TestCertificate.Password);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            AssertNamesTheSettings(exception, CertificateBase64Key);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Start_WithoutACertificatePassword_ThrowsAnInvalidOperationExceptionNamingTheSetting(string? certificatePassword)
        {
            using KvitApiFactory factory = KvitApiFactory.WithDataProtectionSettings(TestCertificate.Base64, certificatePassword);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            AssertNamesTheSettings(exception, CertificatePasswordKey);
        }

        [Fact]
        public void Start_WithACertificateThatIsNotBase64_ThrowsAnInvalidOperationExceptionNamingBothSettings()
        {
            using KvitApiFactory factory = KvitApiFactory.WithDataProtectionSettings("not-base64!!", TestCertificate.Password);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            AssertNamesTheSettings(exception, CertificateBase64Key, CertificatePasswordKey);
        }

        [Fact]
        public void Start_WithBase64ThatIsNotACertificateFile_ThrowsAnInvalidOperationExceptionNamingBothSettings()
        {
            string notAPfx = Convert.ToBase64String(Encoding.UTF8.GetBytes("hello"));
            using KvitApiFactory factory = KvitApiFactory.WithDataProtectionSettings(notAPfx, TestCertificate.Password);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            AssertNamesTheSettings(exception, CertificateBase64Key, CertificatePasswordKey);
        }

        [Fact]
        public void Start_WithTheWrongCertificatePassword_ThrowsAnInvalidOperationExceptionNamingBothSettings()
        {
            using KvitApiFactory factory = KvitApiFactory.WithDataProtectionSettings(TestCertificate.Base64, TestCertificate.Password + "-wrong");

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            AssertNamesTheSettings(exception, CertificateBase64Key, CertificatePasswordKey);
        }

        [Fact]
        public void Start_WithAValidCertificateAndPassword_Starts()
        {
            using KvitApiFactory factory = KvitApiFactory.WithDataProtectionSettings(TestCertificate.Base64, TestCertificate.Password);

            using HttpClient client = factory.CreateClient();

            Assert.NotNull(client);
        }

        [Fact]
        public void Start_WithAnExpiredCertificate_ThrowsAnInvalidOperationExceptionNamingTheSetting()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            (string base64, string password) = TestCertificate.ValidBetween(now.AddYears(-2), now.AddDays(-1));
            using KvitApiFactory factory = KvitApiFactory.WithDataProtectionSettings(base64, password);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            AssertNamesTheSettings(exception, CertificateBase64Key, "valid only from");
        }

        [Fact]
        public void Start_WithACertificateThatIsNotValidYet_ThrowsAnInvalidOperationExceptionNamingTheSetting()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            (string base64, string password) = TestCertificate.ValidBetween(now.AddDays(1), now.AddYears(1));
            using KvitApiFactory factory = KvitApiFactory.WithDataProtectionSettings(base64, password);

            Exception exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            AssertNamesTheSettings(exception, CertificateBase64Key, "valid only from");
        }

        [Fact]
        public void Start_WithACertificateThatExpiresWithin60Days_StartsAndLogsAWarningWithTheDate()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset notAfter = now.AddDays(30);
            (string base64, string password) = TestCertificate.ValidBetween(now.AddDays(-1), notAfter);
            RecordingLoggerProvider logs = new();
            using KvitApiFactory factory = KvitApiFactory.WithDataProtectionSettings(base64, password);
            factory.AdditionalTestServices = services => services.AddSingleton<ILoggerProvider>(logs);

            using HttpClient client = factory.CreateClient();

            RecordedLogEntry warning = Assert.Single(logs.AllEntries, entry => entry.Message.Contains("login-key certificate", StringComparison.Ordinal));
            Assert.Equal(LogLevel.Warning, warning.Level);
            Assert.Contains(notAfter.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), warning.Message, StringComparison.Ordinal);
            Assert.Contains(CertificateBase64Key, warning.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Start_WithACertificateThatExpiresInMoreThan60Days_LogsNoCertificateWarning()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            (string base64, string password) = TestCertificate.ValidBetween(now.AddDays(-1), now.AddDays(90));
            RecordingLoggerProvider logs = new();
            using KvitApiFactory factory = KvitApiFactory.WithDataProtectionSettings(base64, password);
            factory.AdditionalTestServices = services => services.AddSingleton<ILoggerProvider>(logs);

            using HttpClient client = factory.CreateClient();

            Assert.DoesNotContain(logs.AllEntries, entry => entry.Message.Contains("login-key certificate", StringComparison.Ordinal));
        }

        private static void AssertNamesTheSettings(Exception exception, params string[] settingNames)
        {
            InvalidOperationException? invalidOperation = ExceptionChain.Of(exception)
                .OfType<InvalidOperationException>()
                .FirstOrDefault(candidate => settingNames.All(name => candidate.Message.Contains(name)));

            Assert.True(
                invalidOperation is not null,
                $"No InvalidOperationException in the chain names {string.Join(" and ", settingNames)}. Got: {exception.GetType().Name}: {exception.Message}");
        }
    }
}
