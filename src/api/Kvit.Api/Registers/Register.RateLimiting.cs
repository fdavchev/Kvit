using System.Threading.RateLimiting;
using Kvit.Api.RateLimiting;
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
                options.OnRejected = (context, _) => new ValueTask(RateLimitedAnswer.WriteAsync(context.HttpContext, context.Lease));
                options.AddPolicy("log-in", httpContext =>
                    FixedWindowPerVisitor(httpContext, LogInAttemptsPerWindow, TimeSpan.FromMinutes(LogInWindowMinutes)));
            });

            services.AddSingleton(_ => new SignUpLimitFilter(PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                FixedWindowPerVisitor(httpContext, SignUpAttemptsPerWindow, TimeSpan.FromMinutes(SignUpWindowMinutes)))));

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
    }
}
