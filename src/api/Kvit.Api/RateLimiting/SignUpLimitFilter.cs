using System.Threading.RateLimiting;
using Kvit.Contracts.Auth;
using Kvit.Domain.Accounts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kvit.Api.RateLimiting
{
    public sealed class SignUpLimitFilter(PartitionedRateLimiter<HttpContext> _limiter) : IAsyncActionFilter, IDisposable
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            RegisterRequest request = context.ActionArguments.Values.OfType<RegisterRequest>().SingleOrDefault()
                ?? throw new InvalidOperationException(
                    $"{nameof(SignUpLimitFilter)} is on the action '{context.ActionDescriptor.DisplayName}', which takes no {nameof(RegisterRequest)}.");

            if (!NewAccount.Create(request.DisplayName, request.Email, request.Password, request.TimeZone, request.Language).IsSuccess)
            {
                await next();
                return;
            }

            using RateLimitLease lease = _limiter.AttemptAcquire(context.HttpContext);
            if (!lease.IsAcquired)
            {
                await RateLimitedAnswer.WriteAsync(context.HttpContext, lease);
                context.Result = new EmptyResult();
                return;
            }

            await next();
        }

        public void Dispose()
        {
            _limiter.Dispose();
        }
    }
}
