using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.PersonalEntries
{
    public sealed class RestorePersonalEntry(
        IPersonalEntryRepository _entries,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid userId, Guid entryId, CancellationToken cancellationToken)
        {
            Result<PersonalEntry> found = await _entries.FindOwnedIncludingDeletedAsync(userId, entryId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            return found.Value.Restore(_timeProvider.GetUtcNow());
        }
    }
}
