using Kvit.Api.Problems;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Kvit.Api.Authorization
{
    public sealed class AccountStateResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            IEnumerable<IAuthorizationRequirement> failedRequirements = authorizeResult.Forbidden && authorizeResult.AuthorizationFailure is not null
                ? authorizeResult.AuthorizationFailure.FailedRequirements
                : [];

            if (failedRequirements.OfType<NotSuspendedRequirement>().Any())
            {
                return ErrorProblem.WriteAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "This account is suspended by the owner of Kvit.",
                    ResultCodes.ACCOUNT_SUSPENDED);
            }

            if (failedRequirements.OfType<PasswordChangedRequirement>().Any())
            {
                return ErrorProblem.WriteAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "The password was reset. Choose a new password before doing anything else.",
                    ResultCodes.AUTH_MUST_CHANGE_PASSWORD);
            }

            return _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
