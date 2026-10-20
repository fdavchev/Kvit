using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.PersonalEntries
{
    public sealed class DeletePersonalEntry(
        IPersonalEntryRepository _entries,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid userId, Guid entryId, CancellationToken cancellationToken)
        {
            Result<PersonalEntry> found = await _entries.FindOwnedAsync(userId, entryId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            found.Value.Delete(_timeProvider.GetUtcNow());

            return Result.Ok();
        }
    }
}
