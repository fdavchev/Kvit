using Kvit.Application.Commands.Expenses;
using Kvit.Application.Dispatching;
using Kvit.Contracts.Expenses;
using Kvit.Domain.Expenses;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/groups/one-bill")]
    public sealed class OneBillController(IDispatcher _dispatcher) : BaseController
    {
        [HttpPost]
        public async Task<ActionResult<OneBillResponse>> Create(OneBillRequest request, CancellationToken cancellationToken)
        {
            OneBillInput input = new(
                request.Title,
                request.Emoji,
                request.Names,
                request.Note,
                request.AmountMinor,
                request.Currency,
                request.ExpenseDate,
                request.CategoryId,
                request.PaidByPersonIndex,
                request.SplitType,
                [.. request.Shares.Select(share => new PersonShareInput(share.PersonIndex, share.InputValue))]);
            Result<OneBillResponse> result = await _dispatcher.Send<CreateOneBillCommand, OneBillResponse>(new CreateOneBillCommand(request.ClientRequestId, input), cancellationToken);
            return Result(result);
        }
    }
}
