using System.Threading.RateLimiting;
using Kvit.Contracts.Invites;
using Kvit.Domain.Accounts;

namespace Kvit.Api.RateLimiting
{
    public sealed class GuestJoinLimitFilter(PartitionedRateLimiter<HttpContext> limiter) : CheckedRequestLimitFilter<JoinAsGuestRequest>(limiter)
    {
        protected override bool PassesCheapChecks(JoinAsGuestRequest request)
        {
            return GuestJoinChoice.Create(request.ClaimMemberId, request.Name, request.TimeZone, request.Language).IsSuccess;
        }
    }
}
