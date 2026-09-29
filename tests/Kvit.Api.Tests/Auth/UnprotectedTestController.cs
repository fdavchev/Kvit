using Kvit.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Tests.Auth
{
    [Route("api/test")]
    public sealed class UnprotectedTestController : BaseController
    {
        [HttpGet("unprotected")]
        public IActionResult Get() => Content("open");
    }
}
