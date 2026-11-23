using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Api.Problems
{
    public sealed class UnexpectedExceptionHandler(ILogger<UnexpectedExceptionHandler> _logger) : IExceptionHandler
    {
        private const string ServerErrorDetail = "Something went wrong on the server. The cause is in the server log.";
        private const string ChangedMeanwhileDetail = "Someone changed this at the same moment. Try again.";
        private const string RequestTooLargeDetail = "The request body is larger than the server accepts.";
        private const string RequestInvalidDetail = "The server could not read the request.";

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            string request = $"{httpContext.Request.Method} {httpContext.Request.Path}";
            if (exception is BadHttpRequestException badRequest)
            {
                bool isTooLarge = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge;
                string errorCode = isTooLarge ? ResultCodes.REQUEST_TOO_LARGE : ResultCodes.REQUEST_INVALID;
                _logger.LogWarning(exception, "{Request} could not be read; it answers {StatusCode} {ErrorCode}.", request, badRequest.StatusCode, errorCode);
                await ErrorProblem.WriteAsync(httpContext, badRequest.StatusCode, isTooLarge ? RequestTooLargeDetail : RequestInvalidDetail, errorCode);
                return true;
            }

            if (IsSameMomentClash(exception))
            {
                _logger.LogWarning(exception, "{Request} clashed with another request that changed the same rows at the same moment; it answers 409 {ErrorCode}.", request, ResultCodes.CHANGED_MEANWHILE);
                await ErrorProblem.WriteAsync(httpContext, StatusCodes.Status409Conflict, ChangedMeanwhileDetail, ResultCodes.CHANGED_MEANWHILE);
                return true;
            }

            _logger.LogError(exception, "{Request} failed with an unexpected exception; it answers 500 {ErrorCode}.", request, ResultCodes.SERVER_ERROR);
            await ErrorProblem.WriteAsync(httpContext, StatusCodes.Status500InternalServerError, ServerErrorDetail, ResultCodes.SERVER_ERROR);
            return true;
        }

        private static bool IsSameMomentClash(Exception exception)
        {
            return exception is DbUpdateConcurrencyException
                or AccountChangedMeanwhileException
                or MemberChangedMeanwhileException
                or SettlementChangedMeanwhileException
                or ExpenseChangedMeanwhileException
                or BudgetLimitChangedMeanwhileException
                or DeadlockDetectedException
                or ClientRequestIdAlreadySavedException
                or MergeAlreadySavedException
                or RecurringIncomeEntryAlreadySavedException
                || DuplicateSaving.IsDeadlock(exception);
        }
    }
}
