using Kvit.Domain.Accounts;
using Xunit;

namespace Kvit.Domain.Tests.Accounts
{
    public class TermsVersionTests
    {
        private const string Older = "2025-01-01";

        [Fact]
        public void Current_IsTheDateOfTheTermsInForce()
        {
            Assert.Equal("2026-11-04", TermsVersion.Current);
        }

        [Fact]
        public void CurrentIsMaterial_IsFalseForTheFirstVersion()
        {
            Assert.False(TermsVersion.CurrentIsMaterial);
        }

        [Fact]
        public void IsCurrent_TheCurrentVersion_IsTrue()
        {
            Assert.True(TermsVersion.IsCurrent(TermsVersion.Current));
        }

        [Fact]
        public void IsCurrent_NoVersionAccepted_IsFalse()
        {
            Assert.False(TermsVersion.IsCurrent(null));
        }

        [Theory]
        [InlineData(Older)]
        [InlineData("2026-11-03")]
        [InlineData("2026-11-05")]
        [InlineData("")]
        [InlineData(" 2026-11-04")]
        [InlineData("2026-11-04 ")]
        public void IsCurrent_AnyOtherText_IsFalse(string acceptedVersion)
        {
            Assert.False(TermsVersion.IsCurrent(acceptedVersion));
        }

        [Theory]
        [InlineData(Older, "2026-11-04", true, true)]
        [InlineData(Older, "2026-11-04", false, false)]
        [InlineData("2026-11-04", "2026-11-04", true, false)]
        [InlineData("2026-11-04", "2026-11-04", false, false)]
        [InlineData(null, "2026-11-04", true, true)]
        [InlineData(null, "2026-11-04", false, false)]
        public void UpdateIsMaterial_IsTrueOnlyWhenTheVersionDiffersAndTheCurrentOneIsMaterial(string? acceptedVersion, string currentVersion, bool currentIsMaterial, bool expected)
        {
            bool updateIsMaterial = TermsVersion.UpdateIsMaterial(acceptedVersion, currentVersion, currentIsMaterial);

            Assert.Equal(expected, updateIsMaterial);
        }

        [Fact]
        public void UpdateIsMaterial_WithTheRealConstants_IsFalseForEveryoneWhileTheCurrentVersionIsNotMaterial()
        {
            Assert.False(TermsVersion.UpdateIsMaterial(null, TermsVersion.Current, TermsVersion.CurrentIsMaterial));
            Assert.False(TermsVersion.UpdateIsMaterial(Older, TermsVersion.Current, TermsVersion.CurrentIsMaterial));
            Assert.False(TermsVersion.UpdateIsMaterial(TermsVersion.Current, TermsVersion.Current, TermsVersion.CurrentIsMaterial));
        }
    }
}
