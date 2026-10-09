using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Expenses;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Expenses
{
    public sealed class EditExpense(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IExpenseRepository _expenses,
        ICategoryRepository _categories,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, Guid expenseId, ExpenseInput input, ExchangeRateSnapshot? currentRate, CancellationToken cancellationToken)
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

            Expense expense = foundExpense.Value;
            Result allowed = expense.CheckCanBeChangedBy(userId, found.Value.OwnerUserId);
            if (!allowed.IsSuccess)
            {
                return allowed;
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
                return fields;
            }

            Result<string> categoryKey = await _categories.GroupExpenseKeyAsync(fields.Value.CategoryId, cancellationToken);
            if (!categoryKey.IsSuccess)
            {
                return categoryKey;
            }

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            Result<IReadOnlyList<SplitShare>> shares = Expense.SplitAmong(roster, fields.Value, input.PaidByMemberId, input.Shares);
            if (!shares.IsSuccess)
            {
                return shares;
            }

            string storedCategoryKey = await _categories.StoredKeyAsync(expense.CategoryId, cancellationToken);
            ExpenseState before = StateBefore(expense, storedCategoryKey, roster);
            ExpenseState after = StateAfter(fields.Value, categoryKey.Value, input.PaidByMemberId, shares.Value, roster);
            IReadOnlyList<FieldChange> changes = ExpenseChanges.Between(before, after);
            if (changes.Count == 0)
            {
                return Result.Ok();
            }

            if (fields.Value.Amount.Currency != expense.Currency)
            {
                ExchangeRateSnapshot rate = currentRate
                    ?? throw new InvalidOperationException($"Expense {expenseId} changes its currency from {expense.Currency} to {fields.Value.Amount.Currency}, but no current exchange rate was read before the change. Its currency was changed by another request in the meantime.");
                expense.SaveRate(rate);
            }

            expense.Update(fields.Value, input.PaidByMemberId, shares.Value, userId, now);
            _activityEvents.Add(ActivityEvent.ExpenseEdited(groupId, userId, expenseId, changes, fields.Value.Amount.Currency, now));

            return Result.Ok();
        }

        private static ExpenseState StateBefore(Expense expense, string categoryKey, GroupRoster roster)
        {
            IReadOnlyList<ExpenseShare> sharesInJoiningOrder = roster.InJoiningOrder(expense.Shares, share => share.MemberId);
            List<SplitLine> lines = [.. sharesInJoiningOrder.Select(share => new SplitLine(share.MemberId, roster.RowOf(share.MemberId).DisplayName, share.InputValue, share.ShareMinor))];

            return new ExpenseState(
                expense.AmountMinor,
                expense.Currency,
                expense.Title,
                expense.Note,
                expense.ExpenseDate,
                categoryKey,
                expense.PaidByMemberId,
                roster.RowOf(expense.PaidByMemberId).DisplayName,
                expense.SplitType,
                lines);
        }

        private static ExpenseState StateAfter(ExpenseFields fields, string categoryKey, Guid paidByMemberId, IReadOnlyList<SplitShare> sharesInJoiningOrder, GroupRoster roster)
        {
            List<SplitLine> lines = [.. sharesInJoiningOrder.Select(share => new SplitLine(share.MemberId, roster.RowOf(share.MemberId).DisplayName, share.InputValue, share.Share.MinorUnits))];

            return new ExpenseState(
                fields.Amount.MinorUnits,
                fields.Amount.Currency,
                fields.Title,
                fields.Note,
                fields.ExpenseDate,
                categoryKey,
                paidByMemberId,
                roster.RowOf(paidByMemberId).DisplayName,
                fields.SplitType,
                lines);
        }
    }
}
