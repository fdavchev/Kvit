using System.Net;
using System.Net.Sockets;
using Kvit.Api.Proxy;

namespace Kvit.Api.RateLimiting
{
    public static class VisitorAddressKey
    {
        private const int Ipv6PrefixBytes = 8;

        public static string For(IPAddress? address)
        {
            if (address is null)
            {
                throw new InvalidOperationException(
                    "The request has no visitor IP address, so Kvit cannot count its log-in and sign-up attempts. " +
                    $"Behind the proxy the address comes from the {ProxyGateMiddleware.VisitorAddressHeader} header; without the proxy it comes from the connection.");
            }

            IPAddress visitor = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
            if (visitor.AddressFamily != AddressFamily.InterNetworkV6)
            {
                return visitor.ToString();
            }

            byte[] bytes = visitor.GetAddressBytes();
            Array.Clear(bytes, Ipv6PrefixBytes, bytes.Length - Ipv6PrefixBytes);
            return $"{new IPAddress(bytes)}/64";
        }
    }
}
