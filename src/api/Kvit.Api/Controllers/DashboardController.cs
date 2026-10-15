using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Dashboard;
using Kvit.Contracts.Dashboard;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/dashboard")]
    public sealed class DashboardController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken)
        {
            Result<DashboardResponse> result = await _dispatcher.Query<GetDashboardQuery, DashboardResponse>(new GetDashboardQuery(), cancellationToken);
            return Result(result);
        }
    }
}
