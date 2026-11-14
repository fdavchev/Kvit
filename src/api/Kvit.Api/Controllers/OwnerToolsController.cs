using Kvit.Application.Commands.OwnerTools;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.OwnerTools;
using Kvit.Contracts.OwnerTools;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Route("api/admin/tools")]
    public sealed class OwnerToolsController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet("people")]
        [Authorize(Policy = Permissions.OwnerToolsUse)]
        public async Task<ActionResult<IReadOnlyList<OwnerToolsPersonRow>>> SearchPeople([FromQuery] string? query, CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<OwnerToolsPersonRow>> result = await _dispatcher.Query<SearchPeopleQuery, IReadOnlyList<OwnerToolsPersonRow>>(new SearchPeopleQuery(query), cancellationToken);
            return Result(result);
        }

        [HttpPost("people/{personId:guid}/suspend")]
        [Authorize(Policy = Permissions.OwnerToolsUse)]
        public async Task<ActionResult> Suspend(Guid personId, OwnerToolsReasonRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new SuspendPersonCommand(personId, request.Reason), cancellationToken);
            return Result(result);
        }

        [HttpPost("people/{personId:guid}/unsuspend")]
        [Authorize(Policy = Permissions.OwnerToolsUse)]
        public async Task<ActionResult> Unsuspend(Guid personId, OwnerToolsReasonRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new UnsuspendPersonCommand(personId, request.Reason), cancellationToken);
            return Result(result);
        }

        [HttpPost("people/{personId:guid}/erase-name")]
        [Authorize(Policy = Permissions.OwnerToolsUse)]
        public async Task<ActionResult> EraseName(Guid personId, OwnerToolsReasonRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new ErasePersonNameCommand(personId, request.Reason), cancellationToken);
            return Result(result);
        }

        [HttpGet("groups/{groupId:guid}")]
        [Authorize(Policy = Permissions.OwnerToolsUse)]
        public async Task<ActionResult<OwnerToolsGroupResponse>> GetGroup(Guid groupId, CancellationToken cancellationToken)
        {
            Result<OwnerToolsGroupResponse> result = await _dispatcher.Query<GetOwnerToolsGroupQuery, OwnerToolsGroupResponse>(new GetOwnerToolsGroupQuery(groupId), cancellationToken);
            return Result(result);
        }

        [HttpPost("groups/{groupId:guid}/close")]
        [Authorize(Policy = Permissions.OwnerToolsUse)]
        public async Task<ActionResult> CloseGroup(Guid groupId, OwnerToolsReasonRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new CloseGroupAsAdminCommand(groupId, request.Reason), cancellationToken);
            return Result(result);
        }

        [HttpPost("groups/{groupId:guid}/reopen")]
        [Authorize(Policy = Permissions.OwnerToolsUse)]
        public async Task<ActionResult> ReopenGroup(Guid groupId, OwnerToolsReasonRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new ReopenGroupAsAdminCommand(groupId, request.Reason), cancellationToken);
            return Result(result);
        }

        [HttpGet("log")]
        [Authorize(Policy = Permissions.OwnerToolsUse)]
        public async Task<ActionResult<IReadOnlyList<OwnerToolsLogRow>>> Log([FromQuery] int? limit, CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<OwnerToolsLogRow>> result = await _dispatcher.Query<GetOwnerToolsLogQuery, IReadOnlyList<OwnerToolsLogRow>>(new GetOwnerToolsLogQuery(limit), cancellationToken);
            return Result(result);
        }
    }
}
