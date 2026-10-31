using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.PersonalEntries
{
    public sealed class EditSpendingEntry(
        EditPersonalEntry _editPersonalEntry,
        IPersonalEntryRepository _entries,
        IDuplicateAnswerRepository _answers)
    {
        public async Task<Result> Execute(Guid userId, Guid entryId, PersonalEntryInput input, CancellationToken cancellationToken)
        {
            Result<PersonalEntry> found = await _entries.FindOwnedAsync(userId, entryId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            PersonalEntry entry = found.Value;
            DuplicateMatchFields before = entry.FieldsDuplicatesMatchOn();
            Result edited = await _editPersonalEntry.Execute(userId, entryId, input, cancellationToken);
            if (!edited.IsSuccess)
            {
                return edited;
            }

            if (entry.FieldsDuplicatesMatchOn() != before)
            {
                await _answers.RemoveSameAnswersOfEntryAsync(userId, entryId, cancellationToken);
            }

            return Result.Ok();
        }
    }
}
