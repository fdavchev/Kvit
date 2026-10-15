using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Domain.Services.Expenses
{
    public sealed class DeleteExpense(
        IGroupRepository _groups,
        IExpenseRepository _expenses,
        IActivityEventRepository _activityEvents,
        CancelClosingOnChange _cancelClosingOnChange,
        FinishPaidOneBill _finishPaidOneBill,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, Guid expenseId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Result<Expense> foundExpense = await _expenses.FindInGroupAsync(groupId, expenseId, cancellationToken);
            if (!foundExpense.IsSuccess)
            {
                return foundExpense;
            }

            Group group = found.Value;
            Expense expense = foundExpense.Value;
            Result allowed = expense.CheckCanBeChangedBy(userId, group.OwnerUserId);
            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Result changeable = await _cancelClosingOnChange.Execute(group, userId, cancellationToken);
            if (!changeable.IsSuccess)
            {
                return changeable;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            expense.Delete(userId, now);
            _activityEvents.Add(ActivityEvent.ExpenseDeleted(groupId, userId, expenseId, expense.Title, expense.AmountMinor, expense.Currency, now));
            await _finishPaidOneBill.Execute(group, userId, cancellationToken);

            return Result.Ok();
        }
    }
}
