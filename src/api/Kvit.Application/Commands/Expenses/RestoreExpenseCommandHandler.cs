using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Expenses;

namespace Kvit.Application.Commands.Expenses
{
    public sealed class RestoreExpenseCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        RestoreExpense _restoreExpense,
        IUnitOfWork _unitOfWork) : ICommandHandler<RestoreExpenseCommand>
    {
        public async Task<Result> Handle(RestoreExpenseCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _restoreExpense.Execute(command.GroupId, userId.Value, command.ExpenseId, cancellationToken), cancellationToken);
        }
    }
}
