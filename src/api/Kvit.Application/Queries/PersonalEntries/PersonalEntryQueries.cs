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

        public static IQueryable<PersonalEntry> EntriesOf(this AppDbContext context, Guid userId, DateOnly firstDay, DateOnly firstDayAfter)
        {
            return context.EntriesOf(userId)
                .Where(entry => entry.EntryDate >= firstDay && entry.EntryDate < firstDayAfter);
        }

        public static IQueryable<PersonalEntryDto> PersonalEntryDtos(this IQueryable<PersonalEntry> entries, AppDbContext context, Guid userId)
        {
            IQueryable<DuplicateAnswer> merges = context.MergesOf(userId);

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
                    merges
                        .Where(merge => merge.PersonalEntryId == entry.Id)
                        .OrderBy(merge => merge.CreatedAt)
                        .ThenBy(merge => merge.Id)
                        .Select(merge => (Guid?)merge.ExpenseId)
                        .FirstOrDefault(),
                    entry.CreatedAt,
                    entry.UpdatedAt));
        }

        public static IQueryable<GroupShareDto> GroupSharesOf(this AppDbContext context, Guid userId, DateOnly firstDay, DateOnly firstDayAfter)
        {
            return context.CountedSharesOf(userId)
                .AsNoTracking()
                .Where(row => row.Expense.ExpenseDate >= firstDay && row.Expense.ExpenseDate < firstDayAfter)
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
                    row.Expense.MkdPerEur,
                    row.Expense.AmountMinor,
                    context.GroupMembers.Any(member => member.Id == row.Expense.PaidByMemberId && member.UserId == userId)));
        }

        public static Task<List<PersonalCategoryRow>> PersonalCategoryRowsOfAsync(this AppDbContext context, IReadOnlyCollection<PersonalEntryDto> entries, CancellationToken cancellationToken)
        {
            return context.PersonalCategoryRowsOfAsync(entries.Select(entry => entry.CategoryId), cancellationToken);
        }

        public static Task<List<PersonalCategoryRow>> PersonalCategoryRowsOfAsync(this AppDbContext context, IEnumerable<Guid?> optionalCategoryIds, CancellationToken cancellationToken)
        {
            List<Guid> categoryIds = [.. optionalCategoryIds.OfType<Guid>().Distinct()];

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
                entry.MergedWithExpenseId,
                entry.CreatedAt,
                entry.UpdatedAt);
        }
    }
}
