using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Expenses;

namespace Kvit.Application.Commands.Expenses
{
    public sealed class EditExpenseCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        EditExpense _editExpense,
        GroupWriteTransaction _groupWrite) : ICommandHandler<EditExpenseCommand>
    {
        public async Task<Result> Handle(EditExpenseCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunOrAnswerIfExpenseChangedMeanwhileAsync(
                command.GroupId,
                userId.Value,
                () => _editExpense.Execute(command.GroupId, userId.Value, command.ExpenseId, command.Input, cancellationToken),
                () => Task.FromResult(Expense.ChangedMeanwhile(command.ExpenseId)),
                cancellationToken);
        }
    }
}
