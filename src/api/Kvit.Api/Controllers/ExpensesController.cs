using Kvit.Application.Commands.Expenses;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Expenses;
using Kvit.Contracts.Expenses;
using Kvit.Domain.Expenses;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/groups/{groupId:guid}/expenses")]
    public sealed class ExpensesController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<ExpenseListResponse>> List(Guid groupId, CancellationToken cancellationToken)
        {
            Result<ExpenseListResponse> result = await _dispatcher.Query<GetExpensesQuery, ExpenseListResponse>(new GetExpensesQuery(groupId), cancellationToken);
            return Result(result);
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseDetailResponse>> Add(Guid groupId, AddExpenseRequest request, CancellationToken cancellationToken)
        {
            ExpenseInput input = new(
                request.Title,
                request.Note,
                request.AmountMinor,
                request.Currency,
                request.ExpenseDate,
                request.CategoryId,
                request.PaidByMemberId,
                request.SplitType,
                SplitInputsOf(request.Shares));
            Result<ExpenseDetailResponse> result = await _dispatcher.Send<AddExpenseCommand, ExpenseDetailResponse>(new AddExpenseCommand(groupId, request.ClientRequestId, input), cancellationToken);
            return Result(result);
        }

        [HttpGet("deleted")]
        public async Task<ActionResult<DeletedExpenseListResponse>> ListDeleted(Guid groupId, CancellationToken cancellationToken)
        {
            Result<DeletedExpenseListResponse> result = await _dispatcher.Query<GetDeletedExpensesQuery, DeletedExpenseListResponse>(new GetDeletedExpensesQuery(groupId), cancellationToken);
            return Result(result);
        }

        [HttpGet("{expenseId:guid}")]
        public async Task<ActionResult<ExpenseDetailResponse>> Get(Guid groupId, Guid expenseId, CancellationToken cancellationToken)
        {
            Result<ExpenseDetailResponse> result = await _dispatcher.Query<GetExpenseQuery, ExpenseDetailResponse>(new GetExpenseQuery(groupId, expenseId), cancellationToken);
            return Result(result);
        }

        [HttpPut("{expenseId:guid}")]
        public async Task<ActionResult> Edit(Guid groupId, Guid expenseId, EditExpenseRequest request, CancellationToken cancellationToken)
        {
            ExpenseInput input = new(
                request.Title,
                request.Note,
                request.AmountMinor,
                request.Currency,
                request.ExpenseDate,
                request.CategoryId,
                request.PaidByMemberId,
                request.SplitType,
                SplitInputsOf(request.Shares));
            Result result = await _dispatcher.Send(new EditExpenseCommand(groupId, expenseId, input), cancellationToken);
            return Result(result);
        }

        [HttpDelete("{expenseId:guid}")]
        public async Task<ActionResult> Delete(Guid groupId, Guid expenseId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new DeleteExpenseCommand(groupId, expenseId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{expenseId:guid}/restore")]
        public async Task<ActionResult> Restore(Guid groupId, Guid expenseId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RestoreExpenseCommand(groupId, expenseId), cancellationToken);
            return Result(result);
        }

        private static List<SplitInput> SplitInputsOf(IReadOnlyList<ShareRequest> shares)
        {
            return [.. shares.Select(share => new SplitInput(share.MemberId, share.InputValue))];
        }
    }
}
