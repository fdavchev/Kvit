using Kvit.Domain.Results;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        public const string ErrorCodeExtensionKey = "errorCode";

        protected ActionResult Result(Result result)
        {
            if (!result.IsSuccess)
            {
                return ProblemFor(result);
            }

            return NoContent();
        }

        protected ActionResult<T> Result<T>(Result<T> result)
        {
            if (!result.IsSuccess)
            {
                return ProblemFor(result);
            }

            return Ok(result.Value);
        }

        private ObjectResult ProblemFor(Result result)
        {
            Dictionary<string, object?> extensions = new() { [ErrorCodeExtensionKey] = result.ErrorCode };
            return Problem(detail: result.Error, statusCode: (int)result.StatusCode, extensions: extensions);
        }
    }
}
