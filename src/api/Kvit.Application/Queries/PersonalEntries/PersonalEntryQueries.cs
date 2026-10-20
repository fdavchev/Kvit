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
                    entry.CreatedAt,
                    entry.UpdatedAt));
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
                entry.CreatedAt,
                entry.UpdatedAt);
        }
    }
}
