using Kvit.Domain.Accounts;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Domain.Tests.Accounts
{
    public class NewGoogleAccountTests
    {
        private const string ValidName = "Ana";
        private const string ValidEmail = "ana@example.com";
        private const string ValidTimeZone = "Europe/Skopje";
        private const string ValidLanguage = "en";

        [Fact]
        public void Create_ValidData_ReturnsTheAccountWithTheNameAndTheEmailTrimmed()
        {
            Result<NewGoogleAccount> result = NewGoogleAccount.Create("  Ана Петровска  ", "  ana@example.com  ", "America/New_York", "mk");

            Assert.True(result.IsSuccess);
            Assert.Equal("Ана Петровска", result.Value.DisplayName);
            Assert.Equal("ana@example.com", result.Value.Email);
            Assert.Equal("America/New_York", result.Value.TimeZone);
            Assert.Equal("mk", result.Value.Language);
        }

        [Fact]
        public void Create_DisplayNameOf60Characters_Succeeds()
        {
            string displayName = new('a', 60);

            Result<NewGoogleAccount> result = NewGoogleAccount.Create(displayName, ValidEmail, ValidTimeZone, ValidLanguage);

            Assert.True(result.IsSuccess);
            Assert.Equal(displayName, result.Value.DisplayName);
        }

        [Fact]
        public void Create_DisplayNameOf61Characters_FailsWithDisplayNameInvalid()
        {
            Result<NewGoogleAccount> result = NewGoogleAccount.Create(new string('a', 61), ValidEmail, ValidTimeZone, ValidLanguage);

            AssertFailure(result, ResultCodes.AUTH_DISPLAY_NAME_INVALID);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Create_BlankDisplayName_FailsWithDisplayNameInvalid(string displayName)
        {
            Result<NewGoogleAccount> result = NewGoogleAccount.Create(displayName, ValidEmail, ValidTimeZone, ValidLanguage);

            AssertFailure(result, ResultCodes.AUTH_DISPLAY_NAME_INVALID);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-an-email")]
        [InlineData("a@")]
        [InlineData("a b@c.com")]
        public void Create_InvalidEmail_FailsWithEmailInvalid(string email)
        {
            Result<NewGoogleAccount> result = NewGoogleAccount.Create(ValidName, email, ValidTimeZone, ValidLanguage);

            AssertFailure(result, ResultCodes.AUTH_EMAIL_INVALID);
        }

        [Theory]
        [InlineData("de")]
        [InlineData("EN")]
        [InlineData("")]
        public void Create_UnsupportedLanguage_FailsWithLanguageInvalid(string language)
        {
            Result<NewGoogleAccount> result = NewGoogleAccount.Create(ValidName, ValidEmail, ValidTimeZone, language);

            AssertFailure(result, ResultCodes.LANGUAGE_INVALID);
        }

        [Theory]
        [InlineData("Mars/Olympus")]
        [InlineData("")]
        public void Create_UnknownTimeZone_FailsWithTimeZoneInvalid(string timeZone)
        {
            Result<NewGoogleAccount> result = NewGoogleAccount.Create(ValidName, ValidEmail, timeZone, ValidLanguage);

            AssertFailure(result, ResultCodes.TIME_ZONE_INVALID);
        }

        private static void AssertFailure(Result<NewGoogleAccount> result, string expectedErrorCode)
        {
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedErrorCode, result.ErrorCode);
        }
    }
}
