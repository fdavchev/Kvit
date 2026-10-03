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

        [HttpGet("{groupId:guid}/members")]
        public async Task<ActionResult<GroupMembersResponse>> ListMembers(Guid groupId, CancellationToken cancellationToken)
        {
            Result<GroupMembersResponse> result = await _dispatcher.Query<GetGroupMembersQuery, GroupMembersResponse>(new GetGroupMembersQuery(groupId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{groupId:guid}/members")]
        public async Task<ActionResult<GroupMemberRow>> AddMember(Guid groupId, AddMemberRequest request, CancellationToken cancellationToken)
        {
            AddMemberCommand command = new(groupId, request.Name);
            Result<GroupMemberRow> result = await _dispatcher.Send<AddMemberCommand, GroupMemberRow>(command, cancellationToken);
            return Result(result);
        }

        [HttpDelete("{groupId:guid}/members/{memberId:guid}")]
        public async Task<ActionResult> RemoveMember(Guid groupId, Guid memberId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RemoveMemberCommand(groupId, memberId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{groupId:guid}/members/{memberId:guid}/let-back-in")]
        public async Task<ActionResult> LetBackIn(Guid groupId, Guid memberId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new LetBackInCommand(groupId, memberId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{groupId:guid}/leave")]
        public async Task<ActionResult> Leave(Guid groupId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new LeaveGroupCommand(groupId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{groupId:guid}/owner")]
        public async Task<ActionResult> MakeOwner(Guid groupId, MakeOwnerRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new MakeOwnerCommand(groupId, request.MemberId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{groupId:guid}/members/{memberId:guid}/claim")]
        public async Task<ActionResult> ClaimName(Guid groupId, Guid memberId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new ClaimNameCommand(groupId, memberId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{groupId:guid}/members/{memberId:guid}/undo-claim")]
        public async Task<ActionResult> UndoClaim(Guid groupId, Guid memberId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new UndoClaimCommand(groupId, memberId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{groupId:guid}/invite/reset")]
        public async Task<ActionResult<InviteTokenResponse>> ResetInviteLink(Guid groupId, CancellationToken cancellationToken)
        {
            Result<InviteTokenResponse> result = await _dispatcher.Send<ResetInviteLinkCommand, InviteTokenResponse>(new ResetInviteLinkCommand(groupId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{groupId:guid}/invite/undo-reset")]
        public async Task<ActionResult<InviteTokenResponse>> UndoInviteReset(Guid groupId, CancellationToken cancellationToken)
        {
            Result<InviteTokenResponse> result = await _dispatcher.Send<UndoInviteResetCommand, InviteTokenResponse>(new UndoInviteResetCommand(groupId), cancellationToken);
            return Result(result);
        }
    }
}
