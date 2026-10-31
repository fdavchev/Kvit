using Kvit.Application.Dispatching;
using Kvit.Application.Queries.GroupSpending;
using Kvit.Contracts.GroupSpending;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/groups/{groupId:guid}/spending")]
    public sealed class GroupSpendingController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<GroupSpendingResponse>> Get(Guid groupId, CancellationToken cancellationToken)
        {
            Result<GroupSpendingResponse> result = await _dispatcher.Query<GetGroupSpendingQuery, GroupSpendingResponse>(new GetGroupSpendingQuery(groupId), cancellationToken);
            return Result(result);
        }
    }
}
