using Kvit.Application.Commands.DuplicateAnswers;
using Kvit.Application.Dispatching;
using Kvit.Contracts.DuplicateAnswers;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/personal/duplicate-answers")]
    public sealed class DuplicateAnswersController(IDispatcher _dispatcher) : BaseController
    {
        [HttpPut]
        public async Task<ActionResult> Set(SetDuplicateAnswerRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new SetDuplicateAnswerCommand(request.EntryId, request.ExpenseId, request.AnswerAsText()), cancellationToken);
            return Result(result);
        }

        [HttpDelete]
        public async Task<ActionResult> Remove(RemoveDuplicateAnswerRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RemoveDuplicateAnswerCommand(request.EntryId, request.ExpenseId), cancellationToken);
            return Result(result);
        }
    }
}
