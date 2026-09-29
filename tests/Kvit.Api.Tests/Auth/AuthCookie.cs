using Microsoft.Net.Http.Headers;

namespace Kvit.Api.Tests.Auth
{
    public static class AuthCookie
    {
        public const string Name = "kvit_auth";

        public static SetCookieHeaderValue? Find(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? headers))
            {
                return null;
            }

            List<SetCookieHeaderValue> cookies = [.. headers
                .Select(header => SetCookieHeaderValue.Parse(header))
                .Where(cookie => cookie.Name.Value == Name)];

            return cookies.SingleOrDefault();
        }

        public static DateTimeOffset? ExpiresAt(SetCookieHeaderValue cookie, DateTimeOffset now)
        {
            if (cookie.Expires is not null)
            {
                return cookie.Expires;
            }

            if (cookie.MaxAge is not null)
            {
                return now + cookie.MaxAge.Value;
            }

            return null;
        }
    }
}
