using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kvit.Api.RateLimiting
{
    public abstract class CheckedRequestLimitFilter<TRequest>(PartitionedRateLimiter<HttpContext> _limiter) : IAsyncActionFilter, IDisposable
        where TRequest : class
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            TRequest request = context.ActionArguments.Values.OfType<TRequest>().SingleOrDefault()
                ?? throw new InvalidOperationException(
                    $"{GetType().Name} is on the action '{context.ActionDescriptor.DisplayName}', which takes no {typeof(TRequest).Name}.");

            if (!PassesCheapChecks(request))
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
            GC.SuppressFinalize(this);
        }

        protected abstract bool PassesCheapChecks(TRequest request);
    }
}
