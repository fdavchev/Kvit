using Kvit.Application.Commands.Groups;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Groups;
using Kvit.Contracts.Groups;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/groups")]
    public sealed class GroupsController(IDispatcher _dispatcher) : BaseController
    {
        [HttpPost]
        public async Task<ActionResult<GroupResponse>> Create(CreateGroupRequest request, CancellationToken cancellationToken)
        {
            CreateGroupCommand command = new(request.Name, request.Emoji, request.Currency);
            Result<GroupResponse> result = await _dispatcher.Send<CreateGroupCommand, GroupResponse>(command, cancellationToken);
            return Result(result);
        }

        [HttpGet]
        public async Task<ActionResult<GroupListResponse>> List(CancellationToken cancellationToken)
        {
            Result<GroupListResponse> result = await _dispatcher.Query<GetGroupsQuery, GroupListResponse>(new GetGroupsQuery(), cancellationToken);
            return Result(result);
        }

        [HttpGet("{groupId:guid}")]
        public async Task<ActionResult<GroupResponse>> Get(Guid groupId, CancellationToken cancellationToken)
        {
            Result<GroupResponse> result = await _dispatcher.Query<GetGroupQuery, GroupResponse>(new GetGroupQuery(groupId), cancellationToken);
            return Result(result);
        }

        [HttpPut("{groupId:guid}")]
        public async Task<ActionResult> Update(Guid groupId, UpdateGroupRequest request, CancellationToken cancellationToken)
        {
            UpdateGroupCommand command = new(groupId, request.Name, request.Emoji, request.Currency);
            Result result = await _dispatcher.Send(command, cancellationToken);
            return Result(result);
        }

        [HttpDelete("{groupId:guid}")]
        public async Task<ActionResult> Delete(Guid groupId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new DeleteGroupCommand(groupId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{groupId:guid}/restore")]
        public async Task<ActionResult> Restore(Guid groupId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RestoreGroupCommand(groupId), cancellationToken);
            return Result(result);
        }
    }
}
