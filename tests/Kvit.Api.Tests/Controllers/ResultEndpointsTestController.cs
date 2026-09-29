using Kvit.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Tests.Controllers
{
    [AllowAnonymous]
    [Route("test-results")]
    public sealed class ResultEndpointsTestController : BaseController
    {
        public sealed record Payload(string Name);

        [HttpGet("value")]
        public ActionResult<Payload> Value() => Result(Kvit.Domain.Results.Result.Ok(new Payload("Ana")));

        [HttpGet("null-value")]
        public ActionResult<Payload> NullValue() => Result(Kvit.Domain.Results.Result.Ok<Payload>(null!));
    }
}
