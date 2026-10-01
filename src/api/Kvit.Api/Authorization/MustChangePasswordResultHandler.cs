using Kvit.Api.Problems;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Kvit.Api.Authorization
{
    public sealed class MustChangePasswordResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            bool mustChangePassword = authorizeResult.Forbidden
                && authorizeResult.AuthorizationFailure is not null
                && authorizeResult.AuthorizationFailure.FailedRequirements.OfType<PasswordChangedRequirement>().Any();

            if (!mustChangePassword)
            {
                return _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            }

            return ErrorProblem.WriteAsync(
                context,
                StatusCodes.Status403Forbidden,
                "The password was reset. Choose a new password before doing anything else.",
                ResultCodes.AUTH_MUST_CHANGE_PASSWORD);
        }
    }
}
