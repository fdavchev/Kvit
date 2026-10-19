using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakePersonalEntryRepository : IPersonalEntryRepository
    {
        private readonly List<PersonalEntry> _entries = [];

        public List<PersonalEntry> Added { get; } = [];

        public void Seed(params PersonalEntry[] entries)
        {
            _entries.AddRange(entries);
        }

        public void Add(PersonalEntry entry)
        {
            Added.Add(entry);
        }

        public Task<PersonalEntry?> FindByClientRequestIdAsync(Guid clientRequestId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_entries.SingleOrDefault(entry => entry.ClientRequestId == clientRequestId));
        }

        public Task<Result<PersonalEntry>> FindOwnedAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(SingleOrNotFound(_entries.Where(entry => entry.DeletedAt is null), userId, entryId));
        }

        public Task<Result<PersonalEntry>> FindOwnedIncludingDeletedAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(SingleOrNotFound(_entries, userId, entryId));
        }

        public Task<Currency?> FindCurrencyAsync(Guid userId, Guid entryId, CancellationToken cancellationToken)
        {
            PersonalEntry? entry = _entries.SingleOrDefault(candidate => candidate.Id == entryId && candidate.UserId == userId && candidate.DeletedAt is null);

            return Task.FromResult(entry?.Currency);
        }

        private static Result<PersonalEntry> SingleOrNotFound(IEnumerable<PersonalEntry> entries, Guid userId, Guid entryId)
        {
            PersonalEntry? entry = entries.SingleOrDefault(candidate => candidate.Id == entryId && candidate.UserId == userId);
            if (entry is null)
            {
                return PersonalEntry.NotFound<PersonalEntry>(entryId);
            }

            return Result.Ok(entry);
        }
    }
}
