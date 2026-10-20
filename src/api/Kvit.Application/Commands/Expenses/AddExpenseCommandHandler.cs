using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Expenses;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Expenses;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Expenses;

namespace Kvit.Application.Commands.Expenses
{
    public sealed class AddExpenseCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IDispatcher _dispatcher,
        AddExpense _addExpense,
        GroupWriteTransaction _groupWrite) : ICommandHandler<AddExpenseCommand, ExpenseDetailResponse>
    {
        public async Task<Result<ExpenseDetailResponse>> Handle(AddExpenseCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<ExpenseDetailResponse>();
            }

            Result<Guid> added = await _groupWrite.RunOnceMoreIfClientRequestIdWasTakenAsync(
                command.GroupId,
                userId.Value,
                () => _addExpense.Execute(command.GroupId, userId.Value, command.ClientRequestId, command.Input, cancellationToken),
                cancellationToken);
            if (!added.IsSuccess)
            {
                return added.ToFailure<ExpenseDetailResponse>();
            }

            return await _dispatcher.Query<GetExpenseQuery, ExpenseDetailResponse>(new GetExpenseQuery(command.GroupId, added.Value), cancellationToken);
        }
    }
}
