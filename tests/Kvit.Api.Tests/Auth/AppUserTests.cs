using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class AppUserTests
    {
        private const int VersionDigitIndex = 14;

        [Fact]
        public void NewUser_GetsAVersion7Id()
        {
            AppUser user = new();

            string id = user.Id.ToString("D");
            Assert.Equal('7', id[VersionDigitIndex]);
        }

        [Fact]
        public void NewUser_GetsTheRfc9562VariantBits()
        {
            AppUser user = new();

            string id = user.Id.ToString("D");
            Assert.Contains(id[19], "89ab");
        }

        [Fact]
        public void TwoNewUsers_GetDifferentIds()
        {
            AppUser first = new();
            AppUser second = new();

            Assert.NotEqual(first.Id, second.Id);
        }
    }
}
