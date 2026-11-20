using Kvit.Api.Controllers;
using Kvit.Contracts.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Api.Tests.Problems
{
    [AllowAnonymous]
    [Route("test-exceptions")]
    public sealed class ExceptionEndpointsTestController : BaseController
    {
        public const string SecretDetail = "Host=db.example;Password=never-shown";

        [HttpGet("crash")]
        public ActionResult Crash() => throw new InvalidOperationException($"A bug that must stay in the log: {SecretDetail}");

        [HttpPost("concurrency")]
        public ActionResult Concurrency() => throw new DbUpdateConcurrencyException($"The row changed meanwhile: {SecretDetail}");

        [HttpPost("too-large")]
        public ActionResult TooLarge() => throw new BadHttpRequestException($"Request body too large: {SecretDetail}", StatusCodes.Status413PayloadTooLarge);

        [HttpPost("unreadable")]
        public ActionResult Unreadable() => throw new BadHttpRequestException($"Unexpected end of request content: {SecretDetail}");

        [HttpPost("member-changed-twice")]
        public ActionResult MemberChangedTwice() => throw new MemberChangedMeanwhileException(new DbUpdateConcurrencyException(SecretDetail));
    }
}
