using Kvit.Application.Commands.Invites;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Invites;
using Kvit.Contracts.Invites;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/invites")]
    public sealed class InvitesController(IDispatcher _dispatcher) : BaseController
    {
        [HttpPost("preview")]
        [AllowAnonymous]
        [EnableRateLimiting("invite")]
        public async Task<ActionResult<InvitePreviewResponse>> Preview(PreviewInviteRequest request, CancellationToken cancellationToken)
        {
            Result<InvitePreviewResponse> result = await _dispatcher.Query<PreviewInviteQuery, InvitePreviewResponse>(new PreviewInviteQuery(request.Token), cancellationToken);
            return Result(result);
        }

        [HttpPost("join")]
        [EnableRateLimiting("invite")]
        public async Task<ActionResult<JoinGroupResponse>> Join(JoinGroupRequest request, CancellationToken cancellationToken)
        {
            JoinGroupCommand command = new(request.Token, request.ClaimMemberId);
            Result<JoinGroupResponse> result = await _dispatcher.Send<JoinGroupCommand, JoinGroupResponse>(command, cancellationToken);
            return Result(result);
        }
    }
}
