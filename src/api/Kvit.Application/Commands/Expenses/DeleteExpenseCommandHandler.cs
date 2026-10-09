using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Expenses;

namespace Kvit.Application.Commands.Expenses
{
    public sealed class DeleteExpenseCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        DeleteExpense _deleteExpense,
        IUnitOfWork _unitOfWork) : ICommandHandler<DeleteExpenseCommand>
    {
        public async Task<Result> Handle(DeleteExpenseCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _deleteExpense.Execute(command.GroupId, userId.Value, command.ExpenseId, cancellationToken), cancellationToken);
        }
    }
}
