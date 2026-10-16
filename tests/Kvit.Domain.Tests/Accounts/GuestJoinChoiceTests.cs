using Kvit.Domain.Accounts;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Domain.Tests.Accounts
{
    public class GuestJoinChoiceTests
    {
        private const string ValidTimeZone = "Europe/Skopje";
        private const string ValidLanguage = "en";

        private static readonly Guid FreeNameId = Guid.Parse("0199a1b2-c3d4-7e5f-8a6b-7c8d9e0f1a40");

        [Fact]
        public void Create_AFreeNameToClaim_ReturnsTheClaimAndNoNewName()
        {
            Result<GuestJoinChoice> result = GuestJoinChoice.Create(FreeNameId, null, "America/New_York", "mk");

            Assert.True(result.IsSuccess);
            Assert.Equal(FreeNameId, result.Value.ClaimMemberId);
            Assert.Null(result.Value.NewName);
            Assert.Equal("America/New_York", result.Value.TimeZone);
            Assert.Equal("mk", result.Value.Language);
        }

        [Fact]
        public void Create_ANewName_ReturnsTheNameTrimmedAndNoClaim()
        {
            Result<GuestJoinChoice> result = GuestJoinChoice.Create(null, "  Цветан  ", ValidTimeZone, ValidLanguage);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value.ClaimMemberId);
            Assert.Equal("Цветан", result.Value.NewName);
        }

        [Fact]
        public void Create_BothAClaimAndANewName_FailsWithGuestChoiceInvalid()
        {
            Result<GuestJoinChoice> result = GuestJoinChoice.Create(FreeNameId, "Cvetan", ValidTimeZone, ValidLanguage);

            AssertFailure(result, ResultCodes.INVITE_GUEST_CHOICE_INVALID);
        }

        [Fact]
        public void Create_BothAClaimAndABlankName_FailsWithGuestChoiceInvalid()
        {
            Result<GuestJoinChoice> result = GuestJoinChoice.Create(FreeNameId, "", ValidTimeZone, ValidLanguage);

            AssertFailure(result, ResultCodes.INVITE_GUEST_CHOICE_INVALID);
        }

        [Fact]
        public void Create_NeitherAClaimNorANewName_FailsWithGuestChoiceInvalid()
        {
            Result<GuestJoinChoice> result = GuestJoinChoice.Create(null, null, ValidTimeZone, ValidLanguage);

            AssertFailure(result, ResultCodes.INVITE_GUEST_CHOICE_INVALID);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Create_ABlankNewName_FailsWithDisplayNameInvalid(string name)
        {
            Result<GuestJoinChoice> result = GuestJoinChoice.Create(null, name, ValidTimeZone, ValidLanguage);

            AssertFailure(result, ResultCodes.AUTH_DISPLAY_NAME_INVALID);
        }

        [Fact]
        public void Create_ANewNameOf60Characters_Succeeds()
        {
            string name = new('a', 60);

            Result<GuestJoinChoice> result = GuestJoinChoice.Create(null, name, ValidTimeZone, ValidLanguage);

            Assert.True(result.IsSuccess);
            Assert.Equal(name, result.Value.NewName);
        }

        [Fact]
        public void Create_ANewNameOf61Characters_FailsWithDisplayNameInvalid()
        {
            Result<GuestJoinChoice> result = GuestJoinChoice.Create(null, new string('a', 61), ValidTimeZone, ValidLanguage);

            AssertFailure(result, ResultCodes.AUTH_DISPLAY_NAME_INVALID);
        }

        [Theory]
        [InlineData("de")]
        [InlineData("EN")]
        [InlineData("")]
        public void Create_UnsupportedLanguage_FailsWithLanguageInvalid(string language)
        {
            Result<GuestJoinChoice> result = GuestJoinChoice.Create(FreeNameId, null, ValidTimeZone, language);

            AssertFailure(result, ResultCodes.LANGUAGE_INVALID);
        }

        [Theory]
        [InlineData("Mars/Olympus")]
        [InlineData("")]
        public void Create_UnknownTimeZone_FailsWithTimeZoneInvalid(string timeZone)
        {
            Result<GuestJoinChoice> result = GuestJoinChoice.Create(FreeNameId, null, timeZone, ValidLanguage);

            AssertFailure(result, ResultCodes.TIME_ZONE_INVALID);
        }

        private static void AssertFailure(Result<GuestJoinChoice> result, string expectedErrorCode)
        {
            Assert.False(result.IsSuccess);
            Assert.Equal(expectedErrorCode, result.ErrorCode);
        }
    }
}
