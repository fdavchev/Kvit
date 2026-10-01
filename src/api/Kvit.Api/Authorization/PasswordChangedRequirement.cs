using System.Security.Claims;
using Kvit.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Kvit.Api.Authorization
{
    public sealed class PasswordChangedRequirement : AuthorizationHandler<PasswordChangedRequirement>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PasswordChangedRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return Task.CompletedTask;
            }

            if (context.Resource is not HttpContext httpContext)
            {
                throw new InvalidOperationException(
                    $"{nameof(PasswordChangedRequirement)} needs the request's HttpContext as the authorization resource, but got '{context.Resource?.GetType().FullName ?? "null"}'.");
            }

            if (httpContext.GetEndpoint()?.Metadata.GetMetadata<AllowedWithTemporaryPasswordAttribute>() is not null)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            string mustChangePassword = context.User.FindFirstValue(KvitUserClaimsPrincipalFactory.MustChangePasswordClaimType)
                ?? throw new InvalidOperationException(
                    $"The signed-in user {context.User.FindFirstValue(ClaimTypes.NameIdentifier)} has no '{KvitUserClaimsPrincipalFactory.MustChangePasswordClaimType}' claim, " +
                    $"so their login was not built by {nameof(KvitUserClaimsPrincipalFactory)}.");

            if (!bool.Parse(mustChangePassword))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
