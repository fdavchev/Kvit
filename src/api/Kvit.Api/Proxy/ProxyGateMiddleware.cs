using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Kvit.Api.Proxy
{
    public sealed class ProxyGateMiddleware(RequestDelegate _next, string secret)
    {
        public const string VisitorAddressHeader = "X-Kvit-Visitor-Ip";
        private const string SecretHeader = "X-Kvit-Proxy-Secret";

        private readonly byte[] _secretBytes = Encoding.UTF8.GetBytes(secret);

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsHealthCheck(context.Request.Path))
            {
                await _next(context);
                return;
            }

            if (!CarriesTheSecret(context.Request.Headers[SecretHeader]))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            StringValues visitorAddress = context.Request.Headers[VisitorAddressHeader];
            if (visitorAddress.Count != 1 || !IPAddress.TryParse(visitorAddress[0], out IPAddress? _))
            {
                throw new InvalidOperationException(
                    $"A request carried the right proxy secret, but its {VisitorAddressHeader} header is \"{visitorAddress}\", " +
                    "not exactly one IP address, so Kvit cannot tell which visitor sent it. " +
                    "The Cloudflare proxy (src/web/functions/api/[[path]].ts) must set it to the value of cf-connecting-ip.");
            }

            await _next(context);
        }

        private static bool IsHealthCheck(PathString path)
        {
            return path.Equals("/health", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/api/health", StringComparison.OrdinalIgnoreCase);
        }

        private bool CarriesTheSecret(StringValues sentSecret)
        {
            if (sentSecret.Count != 1)
            {
                return false;
            }

            byte[] sentBytes = Encoding.UTF8.GetBytes(sentSecret[0] ?? string.Empty);
            return CryptographicOperations.FixedTimeEquals(sentBytes, _secretBytes);
        }
    }
}
