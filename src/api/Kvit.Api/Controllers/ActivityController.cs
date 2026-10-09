using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Activity;
using Kvit.Contracts.Activity;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/groups/{groupId:guid}/activity")]
    public sealed class ActivityController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<ActivityResponse>> List(Guid groupId, CancellationToken cancellationToken)
        {
            Result<ActivityResponse> result = await _dispatcher.Query<GetActivityQuery, ActivityResponse>(new GetActivityQuery(groupId), cancellationToken);
            return Result(result);
        }
    }
}
