using System.Globalization;
using System.Threading.RateLimiting;
using Kvit.Api.Problems;
using Kvit.Api.RateLimiting;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.RateLimiting;

namespace Kvit.Api.Registers
{
    public static partial class Register
    {
        private const int LogInAttemptsPerWindow = 10;
        private const int LogInWindowMinutes = 1;
        private const int SignUpAttemptsPerWindow = 5;
        private const int SignUpWindowMinutes = 10;

        public static IServiceCollection AddRateLimits(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = (context, _) => AnswerRateLimited(context);
                options.AddPolicy("log-in", httpContext =>
                    FixedWindowPerVisitor(httpContext, LogInAttemptsPerWindow, TimeSpan.FromMinutes(LogInWindowMinutes)));
                options.AddPolicy("sign-up", httpContext =>
                    FixedWindowPerVisitor(httpContext, SignUpAttemptsPerWindow, TimeSpan.FromMinutes(SignUpWindowMinutes)));
            });

            return services;
        }

        private static RateLimitPartition<string> FixedWindowPerVisitor(HttpContext httpContext, int permitLimit, TimeSpan window)
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                VisitorAddressKey.For(httpContext.Connection.RemoteIpAddress),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                });
        }

        private static async ValueTask AnswerRateLimited(OnRejectedContext context)
        {
            if (!context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
            {
                throw new InvalidOperationException(
                    "The rate limiter refused a request without a RetryAfter time on its lease, so Kvit cannot tell the visitor when to try again.");
            }

            int retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
            HttpContext httpContext = context.HttpContext;
            httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

            await ErrorProblem.WriteAsync(
                httpContext,
                StatusCodes.Status429TooManyRequests,
                $"Too many attempts from this address. Try again in {retryAfterSeconds} seconds.",
                ResultCodes.RATE_LIMITED);
        }
    }
}
