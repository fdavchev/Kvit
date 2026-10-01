using System.Globalization;
using System.Threading.RateLimiting;
using Kvit.Api.Problems;
using Kvit.Domain.Results;

namespace Kvit.Api.RateLimiting
{
    public static class RateLimitedAnswer
    {
        public static async Task WriteAsync(HttpContext httpContext, RateLimitLease refusedLease)
        {
            if (!refusedLease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
            {
                throw new InvalidOperationException(
                    "The rate limiter refused a request without a RetryAfter time on its lease, so Kvit cannot tell the visitor when to try again.");
            }

            int retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
            httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

            await ErrorProblem.WriteAsync(
                httpContext,
                StatusCodes.Status429TooManyRequests,
                $"Too many attempts from this address. Try again in {retryAfterSeconds} seconds.",
                ResultCodes.RATE_LIMITED);
        }
    }
}
