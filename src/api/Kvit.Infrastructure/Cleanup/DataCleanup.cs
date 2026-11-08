using Kvit.Contracts.Auth;
using Kvit.Contracts.Cleanup;
using Kvit.Domain.Cleanup;
using Kvit.Domain.Entities;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kvit.Infrastructure.Cleanup
{
    public sealed class DataCleanup(AppDbContext _context, IAccountPurger _accountPurger) : IDataCleanup
    {
        public async Task<CleanupResponse> RunAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            int accountsPurged = await _accountPurger.PurgeDueAccountsAsync(now, cancellationToken);

            DateTimeOffset latestRowDeletion = CleanupRules.LatestRemovableRowDeletion(now);
            int expensesRemoved = await RemoveDeletedExpensesAsync(latestRowDeletion, cancellationToken);
            int settlementsRemoved = await _context.Settlements
                .Where(settlement => settlement.DeletedAt != null && settlement.DeletedAt <= latestRowDeletion)
                .ExecuteDeleteAsync(cancellationToken);
            int personalEntriesRemoved = await RemoveDeletedPersonalEntriesAsync(latestRowDeletion, cancellationToken);

            RemovedGroups removedGroups = await RemoveDeletedGroupsAsync(CleanupRules.LatestRemovableGroupDeletion(now), cancellationToken);

            DateTimeOffset oldestLinkedEvent = CleanupRules.OldestLinkedUsageEvent(now);
            int usageEventsDetached = await _context.UsageEvents
                .Where(usageEvent => usageEvent.UserId != null && usageEvent.OccurredAt < oldestLinkedEvent)
                .ExecuteUpdateAsync(setters => setters.SetProperty(usageEvent => usageEvent.UserId, (Guid?)null), cancellationToken);

            return new CleanupResponse(
                accountsPurged,
                removedGroups.Groups,
                expensesRemoved + removedGroups.Expenses,
                settlementsRemoved + removedGroups.Settlements,
                personalEntriesRemoved,
                usageEventsDetached);
        }

        private async Task<int> RemoveDeletedExpensesAsync(DateTimeOffset latestDeletion, CancellationToken cancellationToken)
        {
            IQueryable<Expense> removable = _context.Expenses.Where(expense => expense.DeletedAt != null && expense.DeletedAt <= latestDeletion);

            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.DuplicateAnswers
                .Where(answer => removable.Any(expense => expense.Id == answer.ExpenseId))
                .ExecuteDeleteAsync(cancellationToken);
            int removed = await removable.ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return removed;
        }

        private async Task<int> RemoveDeletedPersonalEntriesAsync(DateTimeOffset latestDeletion, CancellationToken cancellationToken)
        {
            IQueryable<PersonalEntry> removable = _context.PersonalEntries
                .Where(entry => entry.DeletedAt != null && entry.DeletedAt <= latestDeletion && entry.RecurringIncomeId == null);

            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.DuplicateAnswers
                .Where(answer => removable.Any(entry => entry.Id == answer.PersonalEntryId))
                .ExecuteDeleteAsync(cancellationToken);
            int removed = await removable.ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return removed;
        }

        private async Task<RemovedGroups> RemoveDeletedGroupsAsync(DateTimeOffset latestDeletion, CancellationToken cancellationToken)
        {
            List<Guid> dueGroupIds = await _context.Groups
                .Where(group => group.DeletedAt != null && group.DeletedAt <= latestDeletion)
                .OrderBy(group => group.Id)
                .Select(group => group.Id)
                .ToListAsync(cancellationToken);

            RemovedGroups total = RemovedGroups.None;
            foreach (Guid groupId in dueGroupIds)
            {
                await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                List<Group> lockedGroups = await _context.Groups
                    .FromSql($"SELECT * FROM groups WHERE id = {groupId} FOR UPDATE")
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                if (lockedGroups.SingleOrDefault() is not { DeletedAt: DateTimeOffset deletedAt } || deletedAt > latestDeletion)
                {
                    continue;
                }

                RemovedGroups removed = await RemoveLockedGroupAsync(groupId, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                total = total.Plus(removed);
            }

            return total;
        }

        private async Task<RemovedGroups> RemoveLockedGroupAsync(Guid groupId, CancellationToken cancellationToken)
        {
            IQueryable<Expense> expensesOfGroup = _context.Expenses.Where(expense => expense.GroupId == groupId);

            await _context.DuplicateAnswers
                .Where(answer => expensesOfGroup.Any(expense => expense.Id == answer.ExpenseId))
                .ExecuteDeleteAsync(cancellationToken);
            int expenses = await expensesOfGroup.ExecuteDeleteAsync(cancellationToken);
            int settlements = await _context.Settlements.Where(settlement => settlement.GroupId == groupId).ExecuteDeleteAsync(cancellationToken);
            await _context.ClosingResponses.Where(response => response.GroupId == groupId).ExecuteDeleteAsync(cancellationToken);
            await _context.ActivityEvents.Where(activityEvent => activityEvent.GroupId == groupId).ExecuteDeleteAsync(cancellationToken);
            await _context.GroupMembers.Where(member => member.GroupId == groupId).ExecuteDeleteAsync(cancellationToken);
            int groups = await _context.Groups.Where(group => group.Id == groupId).ExecuteDeleteAsync(cancellationToken);

            return new RemovedGroups(groups, expenses, settlements);
        }
    }
}
