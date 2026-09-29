using System.Security.Claims;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Http;

namespace Kvit.Infrastructure.Auth
{
    public sealed class CurrentUserProvider(IHttpContextAccessor _httpContextAccessor) : ICurrentUserProvider
    {
        public Result<Guid> GetCurrentUserId()
        {
            string? userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null)
            {
                return Result.Unauthorized<Guid>("Nobody is signed in.", ResultCodes.AUTH_NOT_SIGNED_IN);
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                throw new InvalidOperationException($"The signed-in user's {ClaimTypes.NameIdentifier} claim '{userIdClaim}' is not a GUID.");
            }

            return Result.Ok(userId);
        }
    }
}
