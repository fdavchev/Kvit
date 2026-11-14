using System.Security.Claims;
using Kvit.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Kvit.Api.Authorization
{
    public sealed class NotSuspendedRequirement : AuthorizationHandler<NotSuspendedRequirement>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NotSuspendedRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return Task.CompletedTask;
            }

            if (context.Resource is not HttpContext httpContext)
            {
                throw new InvalidOperationException(
                    $"{nameof(NotSuspendedRequirement)} needs the request's HttpContext as the authorization resource, but got '{context.Resource?.GetType().FullName ?? "null"}'.");
            }

            if (httpContext.GetEndpoint()?.Metadata.GetMetadata<AllowedWhileSuspendedAttribute>() is not null)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            string isSuspended = context.User.FindFirstValue(KvitUserClaimsPrincipalFactory.IsSuspendedClaimType)
                ?? throw new InvalidOperationException(
                    $"The signed-in user {context.User.FindFirstValue(ClaimTypes.NameIdentifier)} has no '{KvitUserClaimsPrincipalFactory.IsSuspendedClaimType}' claim, " +
                    $"so their login was not built by {nameof(KvitUserClaimsPrincipalFactory)}.");

            if (!bool.Parse(isSuspended))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
