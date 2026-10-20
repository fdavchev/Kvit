using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class PersonalEntryRepository(AppDbContext _context) : IPersonalEntryRepository
    {
        public void Add(PersonalEntry entry)
        {
            _context.PersonalEntries.Add(entry);
        }

        public Task<PersonalEntry?> FindByClientRequestIdAsync(Guid clientRequestId, CancellationToken cancellationToken)
        {
            return _context.PersonalEntries
                .AsNoTracking()
                .SingleOrDefaultAsync(entry => entry.ClientRequestId == clientRequestId, cancellationToken);
        }

        public Task<Result<PersonalEntry>> FindOwnedAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
        {
            return SingleOrNotFoundAsync(_context.PersonalEntries.Where(entry => entry.DeletedAt == null), userId, entryId, cancellationToken);
        }

        public Task<Result<PersonalEntry>> FindOwnedIncludingDeletedAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
        {
            return SingleOrNotFoundAsync(_context.PersonalEntries, userId, entryId, cancellationToken);
        }

        public Task<bool> AnyFromRecurringIncomeAsync(Guid recurringIncomeId, CancellationToken cancellationToken)
        {
            return _context.PersonalEntries.AnyAsync(entry => entry.RecurringIncomeId == recurringIncomeId, cancellationToken);
        }

        public async Task<IReadOnlyDictionary<Guid, DateOnly>> LastDatesFromRecurringIncomesOfAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _context.PersonalEntries
                .Where(entry => entry.UserId == userId && entry.RecurringIncomeId != null)
                .GroupBy(entry => entry.RecurringIncomeId!.Value)
                .Select(entries => new { RecurringIncomeId = entries.Key, LastDate = entries.Max(entry => entry.EntryDate) })
                .ToDictionaryAsync(last => last.RecurringIncomeId, last => last.LastDate, cancellationToken);
        }

        private static async Task<Result<PersonalEntry>> SingleOrNotFoundAsync(IQueryable<PersonalEntry> entries, Guid userId, Guid entryId, CancellationToken cancellationToken)
        {
            PersonalEntry? entry = await entries.SingleOrDefaultAsync(candidate => candidate.Id == entryId && candidate.UserId == userId, cancellationToken);
            if (entry is null)
            {
                return PersonalEntry.NotFound<PersonalEntry>(entryId);
            }

            return Result.Ok(entry);
        }
    }
}
