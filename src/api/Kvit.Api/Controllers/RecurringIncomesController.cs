using Kvit.Application.Commands.RecurringIncomes;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.RecurringIncomes;
using Kvit.Contracts.RecurringIncomes;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/personal/recurring-incomes")]
    public sealed class RecurringIncomesController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<RecurringIncomeListResponse>> List(CancellationToken cancellationToken)
        {
            Result<RecurringIncomeListResponse> result = await _dispatcher.Query<GetRecurringIncomesQuery, RecurringIncomeListResponse>(new GetRecurringIncomesQuery(), cancellationToken);
            return Result(result);
        }

        [HttpPost]
        public async Task<ActionResult<RecurringIncomeRow>> Create(CreateRecurringIncomeRequest request, CancellationToken cancellationToken)
        {
            RecurringIncomeInput input = new(request.AmountMinor, request.Currency, request.Title, request.Frequency, request.DayOfMonth, request.DayOfWeek, request.StartsOn, request.EndsOn);
            Result<RecurringIncomeRow> result = await _dispatcher.Send<CreateRecurringIncomeCommand, RecurringIncomeRow>(new CreateRecurringIncomeCommand(request.ClientRequestId, input), cancellationToken);
            return Result(result);
        }

        [HttpPost("catch-up")]
        public async Task<ActionResult<RecurringIncomeCatchUpResponse>> CatchUp(CancellationToken cancellationToken)
        {
            Result<RecurringIncomeCatchUpResponse> result = await _dispatcher.Send<CatchUpRecurringIncomesCommand, RecurringIncomeCatchUpResponse>(new CatchUpRecurringIncomesCommand(), cancellationToken);
            return Result(result);
        }

        [HttpPut("{recurringIncomeId:guid}")]
        public async Task<ActionResult<RecurringIncomeRow>> Edit(Guid recurringIncomeId, EditRecurringIncomeRequest request, CancellationToken cancellationToken)
        {
            RecurringIncomeInput input = new(request.AmountMinor, request.Currency, request.Title, request.Frequency, request.DayOfMonth, request.DayOfWeek, request.StartsOn, request.EndsOn);
            Result<RecurringIncomeRow> result = await _dispatcher.Send<EditRecurringIncomeCommand, RecurringIncomeRow>(new EditRecurringIncomeCommand(recurringIncomeId, input), cancellationToken);
            return Result(result);
        }

        [HttpPost("{recurringIncomeId:guid}/stop")]
        public async Task<ActionResult> Stop(Guid recurringIncomeId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new StopRecurringIncomeCommand(recurringIncomeId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{recurringIncomeId:guid}/restart")]
        public async Task<ActionResult> Restart(Guid recurringIncomeId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RestartRecurringIncomeCommand(recurringIncomeId), cancellationToken);
            return Result(result);
        }
    }
}
