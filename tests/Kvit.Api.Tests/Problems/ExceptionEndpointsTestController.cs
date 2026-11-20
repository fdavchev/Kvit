using Kvit.Api.Controllers;
using Kvit.Contracts.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

        [HttpPost("raw-sql-deadlock")]
        public ActionResult RawSqlDeadlock() => throw new InvalidOperationException("An exception has been raised that is likely due to a transient failure.", PostgresProblem(PostgresErrorCodes.DeadlockDetected));

        [HttpPost("bare-deadlock")]
        public ActionResult BareDeadlock() => throw PostgresProblem(PostgresErrorCodes.DeadlockDetected);

        [HttpPost("saving-deadlock")]
        public ActionResult SavingDeadlock() => throw new InvalidOperationException("A transient failure.", new DbUpdateException(SecretDetail, PostgresProblem(PostgresErrorCodes.DeadlockDetected)));

        [HttpPost("other-postgres-failure")]
        public ActionResult OtherPostgresFailure() => throw new InvalidOperationException("A transient failure.", PostgresProblem(PostgresErrorCodes.SerializationFailure));

        private static PostgresException PostgresProblem(string sqlState) => new($"Postgres failed: {SecretDetail}", "ERROR", "ERROR", sqlState);
    }
}
