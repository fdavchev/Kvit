using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Balances;
using Kvit.Contracts.Balances;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/groups/{groupId:guid}/balances")]
    public sealed class BalancesController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<BalancesResponse>> Get(Guid groupId, CancellationToken cancellationToken)
        {
            Result<BalancesResponse> result = await _dispatcher.Query<GetBalancesQuery, BalancesResponse>(new GetBalancesQuery(groupId), cancellationToken);
            return Result(result);
        }
    }
}
