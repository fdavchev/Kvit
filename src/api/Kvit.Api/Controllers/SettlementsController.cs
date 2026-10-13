using Kvit.Application.Commands.Settlements;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Settlements;
using Kvit.Contracts.Settlements;
using Kvit.Domain.Results;
using Kvit.Domain.Settlements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/groups/{groupId:guid}/settlements")]
    public sealed class SettlementsController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<SettlementListResponse>> List(Guid groupId, CancellationToken cancellationToken)
        {
            Result<SettlementListResponse> result = await _dispatcher.Query<GetSettlementsQuery, SettlementListResponse>(new GetSettlementsQuery(groupId), cancellationToken);
            return Result(result);
        }

        [HttpPost]
        public async Task<ActionResult<SettlementResponse>> Record(Guid groupId, RecordSettlementRequest request, CancellationToken cancellationToken)
        {
            SettlementInput input = new(request.FromMemberId, request.ToMemberId, request.AmountMinor, request.Currency);
            Result<SettlementResponse> result = await _dispatcher.Send<RecordSettlementCommand, SettlementResponse>(new RecordSettlementCommand(groupId, request.ClientRequestId, input), cancellationToken);
            return Result(result);
        }

        [HttpPost("{settlementId:guid}/confirm")]
        public async Task<ActionResult> Confirm(Guid groupId, Guid settlementId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new ConfirmSettlementCommand(groupId, settlementId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{settlementId:guid}/reject")]
        public async Task<ActionResult> Reject(Guid groupId, Guid settlementId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RejectSettlementCommand(groupId, settlementId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{settlementId:guid}/cancel")]
        public async Task<ActionResult> Cancel(Guid groupId, Guid settlementId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new CancelSettlementCommand(groupId, settlementId), cancellationToken);
            return Result(result);
        }

        [HttpDelete("{settlementId:guid}")]
        public async Task<ActionResult> Delete(Guid groupId, Guid settlementId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new DeleteSettlementCommand(groupId, settlementId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{settlementId:guid}/restore")]
        public async Task<ActionResult> Restore(Guid groupId, Guid settlementId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RestoreSettlementCommand(groupId, settlementId), cancellationToken);
            return Result(result);
        }
    }
}
