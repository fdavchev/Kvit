using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Expenses;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Expenses
{
    public sealed class AddExpense(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IExpenseRepository _expenses,
        ICategoryRepository _categories,
        IActivityEventRepository _activityEvents,
        IUsageEventRepository _usageEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result<Guid>> Execute(Guid groupId, Guid userId, Guid clientRequestId, ExpenseInput input, ExchangeRateSnapshot rate, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found.ToFailure<Guid>();
            }

            Expense? earlier = await _expenses.FindByClientRequestIdAsync(clientRequestId, cancellationToken);
            if (earlier is not null)
            {
                return earlier.GroupId == groupId && earlier.CreatedByUserId == userId
                    ? Result.Ok(earlier.Id)
                    : Expense.ClientRequestIdUsed<Guid>(clientRequestId);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<ExpenseFields> fields = ExpenseFields.Create(
                input.Title,
                input.Note,
                input.AmountMinor,
                input.Currency,
                input.ExpenseDate,
                input.CategoryId,
                input.SplitType,
                DateOnly.FromDateTime(now.UtcDateTime),
                ExpenseFields.TitleMaxLength);
            if (!fields.IsSuccess)
            {
                return fields.ToFailure<Guid>();
            }

            Result<string> categoryKey = await _categories.GroupExpenseKeyAsync(fields.Value.CategoryId, cancellationToken);
            if (!categoryKey.IsSuccess)
            {
                return categoryKey.ToFailure<Guid>();
            }

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            Result<IReadOnlyList<SplitShare>> shares = Expense.SplitAmong(roster, fields.Value, input.PaidByMemberId, input.Shares);
            if (!shares.IsSuccess)
            {
                return shares.ToFailure<Guid>();
            }

            Expense expense = Expense.Create(groupId, fields.Value, input.PaidByMemberId, shares.Value, rate, userId, clientRequestId, now);
            _expenses.Add(expense);
            _activityEvents.Add(ActivityEvent.ExpenseAdded(groupId, userId, expense.Id, expense.Title, expense.AmountMinor, expense.Currency, now));
            _usageEvents.Add(UsageEvent.ExpenseAdded(userId, now));

            return Result.Ok(expense.Id);
        }
    }
}
