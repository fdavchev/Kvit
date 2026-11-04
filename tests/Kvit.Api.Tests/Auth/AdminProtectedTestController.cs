using Kvit.Api.Controllers;
using Kvit.Domain.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Tests.Auth
{
    [Route("api/test")]
    public sealed class AdminProtectedTestController : BaseController
    {
        [HttpGet("admin-only")]
        [Authorize(Policy = Permissions.AdminStatsView)]
        public IActionResult Get() => Content("admin");
    }
}
