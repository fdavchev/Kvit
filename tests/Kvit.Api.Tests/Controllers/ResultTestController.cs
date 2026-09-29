using Kvit.Api.Controllers;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Tests.Controllers
{
    [AllowAnonymous]
    public sealed class ResultTestController : BaseController
    {
        [NonAction]
        public ActionResult Convert(Result result) => Result(result);

        [NonAction]
        public ActionResult<T> Convert<T>(Result<T> result) => Result(result);
    }
}
