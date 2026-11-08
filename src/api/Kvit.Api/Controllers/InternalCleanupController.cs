using System.Text;
using Kvit.Api.Authorization;
using Kvit.Api.Settings;
using Kvit.Application.Commands.Cleanup;
using Kvit.Application.Dispatching;
using Kvit.Contracts.Cleanup;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kvit.Api.Controllers
{
    [Route("api/internal/cleanup")]
    public sealed class InternalCleanupController(IDispatcher _dispatcher, IConfiguration _configuration) : BaseController
    {
        private const string SecretHeader = "X-Kvit-Cleanup-Secret";

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("cleanup")]
        public async Task<ActionResult<CleanupResponse>> Run(CancellationToken cancellationToken)
        {
            string? secret = CleanupSetting.ReadSecret(_configuration);
            if (secret is null)
            {
                return EmptyAnswer(StatusCodes.Status404NotFound);
            }

            if (!SharedSecret.IsCarriedBy(Request.Headers[SecretHeader], Encoding.UTF8.GetBytes(secret)))
            {
                return EmptyAnswer(StatusCodes.Status403Forbidden);
            }

            Result<CleanupResponse> result = await _dispatcher.Send<RunCleanupCommand, CleanupResponse>(new RunCleanupCommand(), cancellationToken);
            return Result(result);
        }

        private EmptyResult EmptyAnswer(int statusCode)
        {
            Response.StatusCode = statusCode;
            return new EmptyResult();
        }
    }
}
