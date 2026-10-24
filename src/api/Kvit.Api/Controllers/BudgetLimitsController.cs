using Kvit.Application.Commands.BudgetLimits;
using Kvit.Application.Dispatching;
using Kvit.Contracts.BudgetLimits;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/personal/limits")]
    public sealed class BudgetLimitsController(IDispatcher _dispatcher) : BaseController
    {
        [HttpPut("month")]
        public async Task<ActionResult> SetMonth(SetBudgetLimitRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new SetBudgetLimitCommand(null, request.LimitMinorAsWholeNumber()), cancellationToken);
            return Result(result);
        }

        [HttpPut("categories/{categoryId:guid}")]
        public async Task<ActionResult> SetCategory(Guid categoryId, SetBudgetLimitRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new SetBudgetLimitCommand(categoryId, request.LimitMinorAsWholeNumber()), cancellationToken);
            return Result(result);
        }

        [HttpDelete("month")]
        public async Task<ActionResult> RemoveMonth(CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RemoveBudgetLimitCommand(null), cancellationToken);
            return Result(result);
        }

        [HttpDelete("categories/{categoryId:guid}")]
        public async Task<ActionResult> RemoveCategory(Guid categoryId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RemoveBudgetLimitCommand(categoryId), cancellationToken);
            return Result(result);
        }
    }
}
