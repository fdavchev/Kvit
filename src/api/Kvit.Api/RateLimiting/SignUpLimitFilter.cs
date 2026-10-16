using System.Threading.RateLimiting;
using Kvit.Contracts.Auth;
using Kvit.Domain.Accounts;

namespace Kvit.Api.RateLimiting
{
    public sealed class SignUpLimitFilter(PartitionedRateLimiter<HttpContext> limiter) : CheckedRequestLimitFilter<RegisterRequest>(limiter)
    {
        protected override bool PassesCheapChecks(RegisterRequest request)
        {
            return NewAccount.Create(request.DisplayName, request.Email, request.Password, request.TimeZone, request.Language).IsSuccess;
        }
    }
}
