using Kvit.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Kvit.Api.Problems
{
    public static class ErrorProblem
    {
        public static Task WriteAsync(HttpContext httpContext, int statusCode, string detail, string errorCode)
        {
            ProblemDetailsFactory problemDetailsFactory = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            ProblemDetails problem = problemDetailsFactory.CreateProblemDetails(httpContext, statusCode: statusCode, detail: detail);
            problem.Extensions[BaseController.ErrorCodeExtensionKey] = errorCode;

            return TypedResults.Problem(problem).ExecuteAsync(httpContext);
        }
    }
}
