using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Admin;
using Kvit.Contracts.Admin;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Route("api/admin/stats")]
    public sealed class AdminStatsController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        [Authorize(Policy = Permissions.AdminStatsView)]
        public async Task<ActionResult<AdminStatsResponse>> Get(
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            [FromQuery] string? by,
            CancellationToken cancellationToken)
        {
            Result<AdminStatsResponse> result = await _dispatcher.Query<GetAdminStatsQuery, AdminStatsResponse>(new GetAdminStatsQuery(from, to, by), cancellationToken);
            return Result(result);
        }
    }
}
