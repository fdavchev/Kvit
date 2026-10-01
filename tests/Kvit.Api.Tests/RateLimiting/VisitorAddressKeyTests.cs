using System.Net;
using Kvit.Api.RateLimiting;
using Xunit;

namespace Kvit.Api.Tests.RateLimiting
{
    public class VisitorAddressKeyTests
    {
        [Fact]
        public void For_AnIpv4Address_ReturnsItsText()
        {
            string key = VisitorAddressKey.For(IPAddress.Parse("203.0.113.9"));

            Assert.Equal("203.0.113.9", key);
        }

        [Fact]
        public void For_AnIpv4MappedIpv6Address_ReturnsTheSameKeyAsTheIpv4Address()
        {
            string mappedKey = VisitorAddressKey.For(IPAddress.Parse("::ffff:203.0.113.9"));
            string plainKey = VisitorAddressKey.For(IPAddress.Parse("203.0.113.9"));

            Assert.Equal(plainKey, mappedKey);
        }

        [Fact]
        public void For_TwoIpv6AddressesInTheSame64Prefix_ReturnTheSameKey()
        {
            string first = VisitorAddressKey.For(IPAddress.Parse("2001:db8:abcd:12::1"));
            string second = VisitorAddressKey.For(IPAddress.Parse("2001:db8:abcd:12:ffff:ffff:ffff:ffff"));

            Assert.Equal(first, second);
        }

        [Fact]
        public void For_TwoIpv6AddressesInDifferent64Prefixes_ReturnDifferentKeys()
        {
            string first = VisitorAddressKey.For(IPAddress.Parse("2001:db8:abcd:12::1"));
            string second = VisitorAddressKey.For(IPAddress.Parse("2001:db8:abcd:13::1"));

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void For_NoAddress_ThrowsAnInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => VisitorAddressKey.For(null));
        }
    }
}
