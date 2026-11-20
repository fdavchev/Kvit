using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Expenses;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Domain.Services.ExchangeRates;
using Kvit.Domain.Services.Groups;

namespace Kvit.Domain.Services.Expenses
{
    public sealed class EditExpense(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IExpenseRepository _expenses,
        ICategoryRepository _categories,
        IActivityEventRepository _activityEvents,
        CancelClosingOnChange _cancelClosingOnChange,
        FinishPaidOneBill _finishPaidOneBill,
        GetCurrentExchangeRate _getCurrentExchangeRate,
        TimeProvider _timeProvider)
    {
        public async Task<Result<ExchangeRateSnapshot?>> FetchRateIfCurrencyChangesAsync(Guid groupId, Guid userId, Guid expenseId, ExpenseInput input, CancellationToken cancellationToken)
        {
            Result<CheckedExpenseEdit> checkedEdit = await CheckAsync(groupId, userId, expenseId, input, cancellationToken);
            if (!checkedEdit.IsSuccess)
            {
                return checkedEdit.ToFailure<ExchangeRateSnapshot?>();
            }

            if (!checkedEdit.Value.ChangesCurrency)
            {
                return Result.Ok<ExchangeRateSnapshot?>(null);
            }

            Result<ExchangeRateSnapshot> rate = await _getCurrentExchangeRate.Execute(cancellationToken);
            if (!rate.IsSuccess)
            {
                return rate.ToFailure<ExchangeRateSnapshot?>();
            }

            return Result.Ok<ExchangeRateSnapshot?>(rate.Value);
        }

        public async Task<Result> Execute(Guid groupId, Guid userId, Guid expenseId, ExpenseInput input, ExchangeRateSnapshot? newRate, CancellationToken cancellationToken)
        {
            Result<CheckedExpenseEdit> checkedEdit = await CheckAsync(groupId, userId, expenseId, input, cancellationToken);
            if (!checkedEdit.IsSuccess)
            {
                return checkedEdit;
            }

            CheckedExpenseEdit edit = checkedEdit.Value;
            Result changeable = await _cancelClosingOnChange.Execute(edit.Group, userId, cancellationToken);
            if (!changeable.IsSuccess)
            {
                return changeable;
            }

            if (edit.Changes.Count == 0)
            {
                return Result.Ok();
            }

            if (edit.ChangesCurrency)
            {
                if (newRate is null)
                {
                    return Expense.ChangedMeanwhile(expenseId);
                }

                edit.Expense.SaveRate(newRate);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            edit.Expense.Update(edit.Fields, input.PaidByMemberId, edit.Shares, edit.Roster, userId, now);
            _expenses.MarkChanged(edit.Expense);
            _activityEvents.Add(ActivityEvent.ExpenseEdited(groupId, userId, expenseId, edit.Changes, edit.Fields.Amount.Currency, now));
            await _finishPaidOneBill.Execute(edit.Group, userId, cancellationToken);

            return Result.Ok();
        }

        private async Task<Result<CheckedExpenseEdit>> CheckAsync(Guid groupId, Guid userId, Guid expenseId, ExpenseInput input, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found.ToFailure<CheckedExpenseEdit>();
            }

            Result<Expense> foundExpense = await _expenses.FindInGroupAsync(groupId, expenseId, cancellationToken);
            if (!foundExpense.IsSuccess)
            {
                return foundExpense.ToFailure<CheckedExpenseEdit>();
            }

            Group group = found.Value;
            Expense expense = foundExpense.Value;
            Result allowed = expense.CheckCanBeChangedBy(userId, group.OwnerUserId);
            if (!allowed.IsSuccess)
            {
                return allowed.ToFailure<CheckedExpenseEdit>();
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result changeable = group.CheckCanBeChangedBy(userId, now);
            if (!changeable.IsSuccess)
            {
                return changeable.ToFailure<CheckedExpenseEdit>();
            }

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
                return fields.ToFailure<CheckedExpenseEdit>();
            }

            string storedCategoryKey = await _categories.StoredKeyAsync(expense.CategoryId, cancellationToken);
            Result<string> categoryKey = fields.Value.CategoryId == expense.CategoryId
                ? Result.Ok(storedCategoryKey)
                : await _categories.GroupExpenseKeyAsync(fields.Value.CategoryId, cancellationToken);
            if (!categoryKey.IsSuccess)
            {
                return categoryKey.ToFailure<CheckedExpenseEdit>();
            }

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            Result<IReadOnlyList<SplitShare>> shares = expense.SplitAgainAmong(roster, fields.Value, input.PaidByMemberId, input.Shares);
            if (!shares.IsSuccess)
            {
                return shares.ToFailure<CheckedExpenseEdit>();
            }

            ExpenseState before = StateBefore(expense, storedCategoryKey, roster);
            ExpenseState after = StateAfter(fields.Value, categoryKey.Value, input.PaidByMemberId, shares.Value, roster);
            IReadOnlyList<FieldChange> changes = ExpenseChanges.Between(before, after);

            return Result.Ok(new CheckedExpenseEdit(group, expense, fields.Value, roster, shares.Value, changes));
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
