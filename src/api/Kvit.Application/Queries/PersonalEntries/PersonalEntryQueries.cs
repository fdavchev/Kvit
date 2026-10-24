using Kvit.Application.Queries.PersonalCategories;
using Kvit.Contracts.PersonalCategories;
using Kvit.Contracts.PersonalEntries;
using Kvit.Domain.Entities;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.PersonalEntries
{
    public static class PersonalEntryQueries
    {
        public static IQueryable<PersonalEntry> EntriesOf(this AppDbContext context, Guid userId)
        {
            return context.PersonalEntries
                .AsNoTracking()
                .Where(entry => entry.UserId == userId && entry.DeletedAt == null);
        }

        public static IQueryable<PersonalEntryDto> PersonalEntryDtos(this IQueryable<PersonalEntry> entries)
        {
            return entries
                .OrderByDescending(entry => entry.EntryDate)
                .ThenByDescending(entry => entry.CreatedAt)
                .ThenByDescending(entry => entry.Id)
                .Select(entry => new PersonalEntryDto(
                    entry.Id,
                    entry.Kind,
                    entry.CategoryId,
                    entry.AmountMinor,
                    entry.Currency,
                    entry.EntryDate,
                    entry.Note,
                    entry.MkdPerEur,
                    entry.RateDate,
                    entry.RecurringIncomeId,
                    entry.CreatedAt,
                    entry.UpdatedAt));
        }

        public static IQueryable<GroupShareDto> GroupSharesOf(this AppDbContext context, Guid userId, DateOnly firstDay, DateOnly firstDayAfter)
        {
            return context.ExpenseShares
                .AsNoTracking()
                .Where(share => share.CountsInBudget && share.ShareMinor > 0)
                .Join(context.GroupMembers.Where(member => member.UserId == userId), share => share.MemberId, member => member.Id, (share, member) => share)
                .Join(
                    context.Expenses.Where(expense => expense.DeletedAt == null && expense.ExpenseDate >= firstDay && expense.ExpenseDate < firstDayAfter),
                    share => share.ExpenseId,
                    expense => expense.Id,
                    (share, expense) => new { Share = share, Expense = expense })
                .Join(context.Groups.Where(group => group.DeletedAt == null), row => row.Expense.GroupId, group => group.Id, (row, group) => new { row.Share, row.Expense, Group = group })
                .OrderBy(row => row.Expense.ExpenseDate)
                .ThenBy(row => row.Expense.Id)
                .Select(row => new GroupShareDto(
                    new GroupShareRow(
                        row.Expense.Id,
                        row.Group.Id,
                        row.Group.Name,
                        row.Expense.Title,
                        row.Expense.CategoryId,
                        row.Expense.ExpenseDate,
                        row.Expense.Currency,
                        row.Share.ShareMinor),
                    row.Expense.MkdPerEur));
        }

        public static Task<List<PersonalCategoryRow>> PersonalCategoryRowsOfAsync(this AppDbContext context, IReadOnlyCollection<PersonalEntryDto> entries, CancellationToken cancellationToken)
        {
            List<Guid> categoryIds = [.. entries.Select(entry => entry.CategoryId).OfType<Guid>().Distinct()];

            return context.Categories
                .AsNoTracking()
                .Where(category => categoryIds.Contains(category.Id))
                .PersonalCategoryRows()
                .ToListAsync(cancellationToken);
        }

        public static PersonalEntryRow PersonalEntryRowOf(PersonalEntryDto entry, IReadOnlyDictionary<Guid, PersonalCategoryRow> categoriesById)
        {
            return new PersonalEntryRow(
                entry.Id,
                entry.Kind,
                entry.CategoryId,
                entry.CategoryId is Guid categoryId ? categoriesById[categoryId] : null,
                entry.AmountMinor,
                entry.Currency,
                entry.EntryDate,
                entry.Note,
                entry.MkdPerEur,
                entry.RateDate,
                entry.RecurringIncomeId,
                entry.CreatedAt,
                entry.UpdatedAt);
        }
    }
}
